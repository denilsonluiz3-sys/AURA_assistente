using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Android.Speech.Tts;
using TextToSpeech = Android.Speech.Tts.TextToSpeech;

namespace AURA.Mobile.Speech
{
    /// <summary>
    /// TTS nativo Android. Preferido para texto arbitrário em pt-BR.
    /// </summary>
    public sealed class AndroidTtsSpeechService : ISpeechService, IDisposable
    {
        private readonly object _lock = new();
        private readonly ConcurrentDictionary<string, TaskCompletionSource<bool>> _pending = new();
        private TextToSpeech? _tts;
        private bool _initFailed;
        private bool _disposed;

        public bool IsReady
        {
            get { lock (_lock) return _tts != null; }
        }

        public Task InitializeAsync(CancellationToken ct = default)
        {
            lock (_lock)
            {
                if (_tts != null)
                    return Task.CompletedTask;

                if (_initFailed)
                {
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

                // Não captura variável local antes do new — usa campo só no callback
                TextToSpeech? created = null;
                created = new TextToSpeech(activity, new OnInitListener(status =>
                    OnInitCompleted(created, status, tcs)));

                return tcs.Task;
            }
        }

        private void OnInitCompleted(TextToSpeech? tts, OperationResult status, TaskCompletionSource<bool> tcs)
        {
            lock (_lock)
            {
                if (status == OperationResult.Success && tts != null)
                {
                    try
                    {
                        // Volume / tom estáveis (evita “só volume” estranho)
                        tts.SetSpeechRate(1.0f);
                        tts.SetPitch(1.0f);
                    }
                    catch { /* alguns aparelhos ignoram */ }

                    _tts = tts;
                    tcs.TrySetResult(true);
                    return;
                }

                _initFailed = true;
                SafeDispose(tts);
                tcs.TrySetException(new NotSupportedException(
                    "Falha ao inicializar o TTS nativo do Android (status " + status + ")."));
            }
        }

        private static void SafeDispose(TextToSpeech? tts)
        {
            if (tts == null) return;
            try { tts.Stop(); } catch { }
            try
            {
                // Só Dispose se o peer Java existir (evita ArgumentNullException no JniValueManager)
                if (tts.Handle != IntPtr.Zero)
                    tts.Dispose();
            }
            catch (Exception)
            {
                // peer já invalidado pelo runtime
            }
        }

        public async Task SpeakAsync(string text, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            await InitializeAsync(ct).ConfigureAwait(false);

            TextToSpeech tts;
            lock (_lock)
            {
                if (_tts == null)
                    throw new NotSupportedException("TTS nativo do Android não inicializado.");
                tts = _tts;
            }

            var lang = new Java.Util.Locale("pt", "BR");
            if (tts.IsLanguageAvailable(lang) < LanguageAvailableResult.Available)
                lang = Java.Util.Locale.Default;

            tts.SetLanguage(lang);

            string utteranceId = Guid.NewGuid().ToString("N");
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            _pending[utteranceId] = tcs;

            tts.SetOnUtteranceProgressListener(new UtteranceListener(
                id => Complete(id, completed: true),
                id => Complete(id, completed: false)));

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

        private void Complete(string utteranceId, bool completed)
        {
            if (_pending.TryRemove(utteranceId, out var tcs))
                tcs.TrySetResult(completed);
        }

        public Task StopAsync()
        {
            TextToSpeech? tts;
            lock (_lock) { tts = _tts; }
            try { tts?.Stop(); } catch { }
            foreach (var tcs in _pending.Values)
                tcs.TrySetResult(false);
            _pending.Clear();
            return Task.CompletedTask;
        }

        private sealed class OnInitListener : Java.Lang.Object, TextToSpeech.IOnInitListener
        {
            private readonly Action<OperationResult> _onInit;
            public OnInitListener(Action<OperationResult> onInit) => _onInit = onInit;
            public void OnInit(OperationResult status) => _onInit(status);
        }

        private sealed class UtteranceListener : UtteranceProgressListener
        {
            private readonly Action<string> _onDone;
            private readonly Action<string> _onError;
            public UtteranceListener(Action<string> onDone, Action<string> onError)
            {
                _onDone = onDone;
                _onError = onError;
            }
            public override void OnDone(string? utteranceId)
            {
                if (utteranceId != null) _onDone(utteranceId);
            }
#pragma warning disable CS0672
            public override void OnError(string? utteranceId)
            {
                if (utteranceId != null) _onError(utteranceId);
            }
#pragma warning restore CS0672
            public override void OnStart(string? utteranceId) { }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            try { StopAsync().GetAwaiter().GetResult(); } catch { }
            lock (_lock)
            {
                SafeDispose(_tts);
                _tts = null;
            }
        }
    }
}
