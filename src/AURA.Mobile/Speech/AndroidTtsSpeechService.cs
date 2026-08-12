using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Android.Speech.Tts;
using AURA.Mobile.Speech;
using TextToSpeech = Android.Speech.Tts.TextToSpeech;

namespace AURA.Mobile.Speech
{
    /// <summary>
    /// Sintetizador de voz usando o TTS nativo do Android (TextToSpeech).
    /// É o motor preferido da AURA para conversação porque fonemiza texto
    /// arbitrário em pt-br (e qualquer idioma instalado) offline, cobrindo
    /// as respostas reais da IA — que o Kokoro on-device não consegue.
    ///
    /// A sessão é criada sob demanda na primeira fala e reutilizada.
    ///
    /// Ciclo de vida robusto: mantém referência forte ao wrapper gerenciado
    /// (evita que o GC o colete antes do callback JNI OnInit), callback
    /// null-safe que NUNCA lança, dispose idempotente e cancelamento. Falha
    /// de TTS não deve derrubar o Agent Loop — métodos lançam somente
    /// NotSupported/InvalidOperation/OperationCanceled para o fallback decidir.
    /// </summary>
    public sealed class AndroidTtsSpeechService : ISpeechService, IDisposable
    {
        private readonly object _lock = new();
        private readonly ConcurrentDictionary<string, TaskCompletionSource<bool>> _pending = new();

        // Referência forte ao motor ativo (pronto para falar).
        private TextToSpeech? _tts;

        // Referência forte durante a inicialização: mantém o wrapper gerenciado
        // vivo até o callback OnInit disparar (em thread JNI), evitando que o
        // GC colete a referência e o callback chegue com objeto inválido.
        private TextToSpeech? _pendingTts;
        private OnInitListener? _initListener;

        private bool _initFailed;
        private bool _disposed;

        public bool IsReady
        {
            get
            {
                lock (_lock)
                {
                    return _tts != null;
                }
            }
        }

        public Task InitializeAsync(CancellationToken ct = default)
        {
            lock (_lock)
            {
                if (_disposed)
                {
                    return Task.FromException(new ObjectDisposedException(nameof(AndroidTtsSpeechService)));
                }

                if (_tts != null)
                {
                    return Task.CompletedTask;
                }

                if (_initFailed)
                {
                    // Já sabemos que o motor nativo não está disponível:
                    // deixa o fallback (Kokoro) assumir.
                    return Task.FromException(new NotSupportedException(
                        "TTS nativo do Android indisponível neste dispositivo."));
                }

                var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
                if (activity == null)
                {
                    return Task.FromException(new InvalidOperationException(
                        "Sem Activity para criar o TTS nativo do Android."));
                }

                var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

                _initListener = new OnInitListener(status => OnInitCompleted(status, tcs));

                try
                {
                    // Guarda o wrapper gerenciado imediatamente; o listener lê
                    // _pendingTts no momento do callback (não captura a local).
                    _pendingTts = new TextToSpeech(activity, _initListener);
                }
                catch (Exception ex)
                {
                    _initFailed = true;
                    DisposePending();
                    _initListener = null;
                    return Task.FromException(new NotSupportedException(
                        "Falha ao criar o TTS nativo do Android.", ex));
                }

                // Cancela a espera e descarta o motor pendente se o token disparar.
                ct.Register(() =>
                {
                    if (tcs.TrySetCanceled(ct))
                    {
                        lock (_lock)
                        {
                            DisposePending();
                        }
                    }
                });

                return tcs.Task;
            }
        }

        /// <summary>
        /// Chamado pelo TextToSpeech quando o motor termina de inicializar.
        /// Pode rodar em thread JNI; é null-safe e nunca lança.
        /// </summary>
        private void OnInitCompleted(OperationResult status, TaskCompletionSource<bool> tcs)
        {
            try
            {
                lock (_lock)
                {
                    TextToSpeech? tts = _pendingTts;
                    _pendingTts = null;
                    _initListener = null;

                    if (tts == null)
                    {
                        // Motor já foi descartado/cancelado ou callback tardio.
                        tcs.TrySetException(new NotSupportedException(
                            "TTS nativo do Android indisponível (motor descartado)."));
                        return;
                    }

                    if (status == OperationResult.Success)
                    {
                        _tts = tts;
                        tcs.TrySetResult(true);
                    }
                    else
                    {
                        _initFailed = true;
                        TryDispose(tts);
                        tcs.TrySetException(new NotSupportedException(
                            "Falha ao inicializar o TTS nativo do Android (status " + status + ")."));
                    }
                }
            }
            catch (Exception ex)
            {
                // Callback nunca pode derrubar o app.
                tcs.TrySetException(new NotSupportedException(
                    "Erro inesperado ao inicializar o TTS nativo do Android.", ex));
            }
        }

        public async Task SpeakAsync(string text, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            TextToSpeech? tts;
            lock (_lock)
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(nameof(AndroidTtsSpeechService));
                }

                tts = _tts;
            }

            if (tts == null)
            {
                await InitializeAsync(ct).ConfigureAwait(false);
                lock (_lock)
                {
                    tts = _tts;
                }

                if (tts == null)
                {
                    throw new NotSupportedException("TTS nativo do Android não inicializado.");
                }
            }

            try
            {
                // Escolhe português do Brasil se estiver disponível; senão o padrão.
                var lang = new Java.Util.Locale("pt", "BR");
                if (tts.IsLanguageAvailable(lang) < LanguageAvailableResult.Available)
                {
                    lang = Java.Util.Locale.Default;
                }

                tts.SetLanguage(lang);

                string utteranceId = Guid.NewGuid().ToString("N");
                var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                _pending[utteranceId] = tcs;

                tts.SetOnUtteranceProgressListener(new UtteranceListener(
                    (id, _) => Complete(id, completed: true),
                    (id, _) => Complete(id, completed: false)));

                using (ct.Register(() => Complete(utteranceId, completed: false)))
                {
                    OperationResult result = tts.Speak(text, QueueMode.Flush, null, utteranceId);
                    if (result != OperationResult.Success)
                    {
                        _pending.TryRemove(utteranceId, out _);
                        throw new NotSupportedException("TTS nativo recusou falar o texto.");
                    }

                    await tcs.Task.ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (NotSupportedException)
            {
                throw;
            }
            catch (Exception ex)
            {
                // Qualquer falha inesperada do motor vira NotSupportedException para
                // o fallback (Kokoro) decidir — nunca propaga erro cru pro Agent Loop.
                throw new NotSupportedException("Falha ao falar com o TTS nativo.", ex);
            }

            void Complete(string id, bool completed)
            {
                if (_pending.TryRemove(id, out TaskCompletionSource<bool> pending))
                {
                    pending.TrySetResult(completed);
                }
            }
        }

        public Task StopAsync()
        {
            TextToSpeech? tts;
            lock (_lock)
            {
                tts = _tts;
            }

            if (tts != null)
            {
                try
                {
                    tts.Stop();
                }
                catch (Exception)
                {
                    // ignora: motor parou com a Activity
                }
            }

            foreach (TaskCompletionSource<bool> tcs in _pending.Values)
            {
                tcs.TrySetResult(false);
            }

            _pending.Clear();
            return Task.CompletedTask;
        }

        /// <summary>Implementação de OnInitListener (callback de inicialização).</summary>
        private sealed class OnInitListener : Java.Lang.Object, TextToSpeech.IOnInitListener
        {
            private readonly Action<OperationResult> _onInit;

            public OnInitListener(Action<OperationResult> onInit)
            {
                _onInit = onInit;
            }

            public void OnInit(OperationResult status)
            {
                try
                {
                    _onInit(status);
                }
                catch (Exception)
                {
                    // Callback JNI nunca pode lançar para o lado nativo.
                }
            }
        }

        /// <summary>Observa o término de cada utterance para o SpeakAsync poder aguardar.</summary>
        private sealed class UtteranceListener : UtteranceProgressListener
        {
            private readonly Action<string, bool> _onDone;
            private readonly Action<string, bool> _onError;

            public UtteranceListener(Action<string, bool> onDone, Action<string, bool> onError)
            {
                _onDone = onDone;
                _onError = onError;
            }

            public override void OnDone(string? utteranceId)
            {
                if (utteranceId != null)
                {
                    _onDone(utteranceId, true);
                }
            }

            public override void OnError(string? utteranceId)
            {
                if (utteranceId != null)
                {
                    _onError(utteranceId, false);
                }
            }

            public override void OnStart(string? utteranceId)
            {
                // nada a fazer
            }
        }

        private void DisposePending()
        {
            if (_pendingTts != null)
            {
                TryDispose(_pendingTts);
                _pendingTts = null;
            }
        }

        private static void TryDispose(TextToSpeech? tts)
        {
            if (tts == null)
            {
                return;
            }

            try
            {
                tts.Dispose();
            }
            catch (Exception)
            {
                // dispose duplicado/objeto já liberado: ignora.
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            lock (_lock)
            {
                _disposed = true;
                _initListener = null;
                foreach (TaskCompletionSource<bool> tcs in _pending.Values)
                {
                    tcs.TrySetResult(false);
                }

                _pending.Clear();
                TryDispose(_pendingTts);
                _pendingTts = null;
                TryDispose(_tts);
                _tts = null;
            }
        }
    }
}
