using System;
using System.Threading;
using System.Threading.Tasks;
using AURA.Abstractions.Orchestration;
using AURA.Agents;
using AURA.Mobile.Diagnostics;

namespace AURA.Mobile.Speech
{
    /// <summary>
    /// Voz da AURA: TTS da última resposta + STT para comandos naturais.
    /// FAB: toque inicia escuta → intent/orquestrador → fala o resultado.
    /// </summary>
    public sealed class VoiceAssistantService
    {
        private static VoiceAssistantService? _instance;

        private readonly ISpeechService _tts;
        private readonly ISpeechRecognitionService? _stt;
        private readonly IOrchestrator? _orchestrator;
        private readonly IIntentResolver? _intentResolver;
        private readonly object _lock = new();
        private CancellationTokenSource? _cts;
        private bool _listening;

        public string LastUtterance { get; private set; } = string.Empty;

        public bool IsSpeaking
        {
            get { lock (_lock) return _cts != null && !_listening; }
        }

        public bool IsListening
        {
            get { lock (_lock) return _listening; }
        }

        public static VoiceAssistantService? Instance
        {
            get => _instance;
            set => _instance = value;
        }

        public event Action<bool>? ListeningChanged;

        public VoiceAssistantService(
            ISpeechService tts,
            ISpeechRecognitionService? stt = null,
            IOrchestrator? orchestrator = null,
            IIntentResolver? intentResolver = null)
        {
            _tts = tts;
            _stt = stt;
            _orchestrator = orchestrator;
            _intentResolver = intentResolver;
            _instance = this;
        }

        public void SetLastUtterance(string text)
        {
            LastUtterance = text ?? string.Empty;
        }

        /// <summary>
        /// FAB: se falando ou escutando → para; senão inicia STT + comando.
        /// Sem STT disponível, cai no TTS da última resposta.
        /// </summary>
        public async Task ToggleAsync()
        {
            lock (_lock)
            {
                if (_cts != null || _listening)
                {
                    _cts?.Cancel();
                    _stt?.Cancel();
                    _listening = false;
                    return;
                }
            }

            if (_stt != null && _stt.IsAvailable)
            {
                await ListenAndHandleAsync().ConfigureAwait(false);
                return;
            }

            string text = string.IsNullOrWhiteSpace(LastUtterance)
                ? "Estou aqui. Me pergunte qualquer coisa na aba Chat ou diga um comando pela voz quando o microfone estiver disponível."
                : LastUtterance;

            await SpeakAsync(text).ConfigureAwait(false);
        }

        public async Task ListenAndHandleAsync()
        {
            if (_stt == null)
            {
                await SpeakAsync("Reconhecimento de fala indisponível.").ConfigureAwait(false);
                return;
            }

            Stop();

            lock (_lock) { _listening = true; }
            ListeningChanged?.Invoke(true);

            string? heard = null;
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                lock (_lock) { _cts = cts; }

                heard = await _stt.ListenAsync(cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                heard = null;
            }
            catch (Exception ex)
            {
                AuraLog.Exception("VoiceAssistantService.Listen", ex);
                heard = null;
            }
            finally
            {
                lock (_lock)
                {
                    _listening = false;
                    _cts = null;
                }
                ListeningChanged?.Invoke(false);
            }

            if (string.IsNullOrWhiteSpace(heard))
            {
                await SpeakAsync("Não entendi. Tente de novo.").ConfigureAwait(false);
                return;
            }

            AuraLog.Info("STT ouviu: " + heard);
            await HandleCommandAsync(heard).ConfigureAwait(false);
        }

        private async Task HandleCommandAsync(string command)
        {
            try
            {
                if (_intentResolver != null)
                {
                    var intent = _intentResolver.Resolve(command);

                    if (intent.Intent == "navigate"
                        && intent.Parameters.TryGetValue("page", out string? page)
                        && !string.IsNullOrWhiteSpace(page))
                    {
                        string msg = $"Abrindo {page}.";
                        SetLastUtterance(msg);
                        await NavigateAsync(page).ConfigureAwait(false);
                        await SpeakAsync(msg).ConfigureAwait(false);
                        return;
                    }

                    if (_orchestrator != null
                        && (intent.Intent == "android" || intent.Confidence >= 0.85))
                    {
                        string result = await _orchestrator.ExecuteAsync(command).ConfigureAwait(false);
                        string spoken = TruncateForSpeech(result);
                        SetLastUtterance(spoken);
                        await SpeakAsync(spoken).ConfigureAwait(false);
                        return;
                    }
                }

                if (_orchestrator != null)
                {
                    string result = await _orchestrator.ExecuteAsync(command).ConfigureAwait(false);
                    string spoken = TruncateForSpeech(result);
                    SetLastUtterance(spoken);
                    await SpeakAsync(spoken).ConfigureAwait(false);
                    return;
                }

                SetLastUtterance(command);
                await SpeakAsync("Ouvi: " + command).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                AuraLog.Exception("VoiceAssistantService.HandleCommand", ex);
                await SpeakAsync("Erro ao processar o comando.").ConfigureAwait(false);
            }
        }

        private static async Task NavigateAsync(string pageLabel)
        {
            try
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    if (Application.Current?.Windows?.FirstOrDefault()?.Page is MainPage main)
                        await main.NavigateToProcessAsync(pageLabel);
                    else if (Application.Current?.MainPage is MainPage legacy)
                        await legacy.NavigateToProcessAsync(pageLabel);
                }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                AuraLog.Exception("VoiceAssistantService.Navigate", ex);
            }
        }

        private static string TruncateForSpeech(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "Pronto.";
            // Evita ler JSON longo em voz alta
            if (text.TrimStart().StartsWith('{') || text.TrimStart().StartsWith('['))
                return "Diagnóstico concluído. Veja os detalhes na tela Sistema ou Programas.";
            if (text.Length > 280)
                return text.Substring(0, 280) + "…";
            return text;
        }

        public async Task SpeakAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            Stop();

            var cts = new CancellationTokenSource();
            lock (_lock) { _cts = cts; }

            try
            {
                await _tts.InitializeAsync(cts.Token).ConfigureAwait(false);
                await _tts.SpeakAsync(text, cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                AuraLog.Exception("VoiceAssistantService.SpeakAsync", ex);
            }
            finally
            {
                lock (_lock)
                {
                    if (ReferenceEquals(_cts, cts))
                        _cts = null;
                }
                cts.Dispose();
            }
        }

        public void Stop()
        {
            CancellationTokenSource? cts;
            lock (_lock)
            {
                cts = _cts;
                _cts = null;
                _listening = false;
            }

            _stt?.Cancel();

            if (cts != null)
            {
                cts.Cancel();
                cts.Dispose();
            }

            _ = _tts.StopAsync();
            ListeningChanged?.Invoke(false);
        }
    }
}
