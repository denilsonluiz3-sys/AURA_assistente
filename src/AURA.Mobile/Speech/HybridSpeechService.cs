using System;
using System.Threading;
using System.Threading.Tasks;

namespace AURA.Mobile.Speech
{
    /// <summary>
    /// Motor de voz híbrido da AURA:
    ///  1. TTS nativo do Android (fala texto arbitrário, offline, pt-br) — o
    ///     motor padrão para as respostas da IA na conversação.
    ///  2. Kokoro on-device (ONNX) como fallback quando o TTS nativo não
    ///     existe ou falha no dispositivo.
    ///
    /// A UI chama apenas este serviço; a seleção do motor é transparente.
    /// </summary>
    public sealed class HybridSpeechService : ISpeechService
    {
        private readonly AndroidTtsSpeechService _android = new();
        private readonly KokoroSpeechService _kokoro = new();

        public bool IsReady => _android.IsReady || _kokoro.IsReady;

        public async Task InitializeAsync(CancellationToken ct = default)
        {
            try
            {
                await _android.InitializeAsync(ct).ConfigureAwait(false);
            }
            catch (NotSupportedException)
            {
                // TTS nativo indisponível: o Kokoro assume (carregado sob demanda).
            }
            catch (InvalidOperationException)
            {
                // Sem Activity (ex.: início do app): idem.
            }
        }

        public async Task SpeakAsync(string text, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            try
            {
                await _android.SpeakAsync(text, ct).ConfigureAwait(false);
                return;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                // Qualquer falha do motor nativo (incluindo inesperada) cai para o
                // Kokoro. Nunca propaga erro cru para o Agent Loop.
                AuraLog.Warning("TTS nativo indisponível, usando Kokoro: " + ex.Message);
            }

            // Fallback: Kokoro on-device. Textos fora do dicionário do
            // fonemizador lançam NotSupportedException — o chamador decide.
            try
            {
                await _kokoro.InitializeAsync(ct).ConfigureAwait(false);
                await _kokoro.SpeakAsync(text, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                // O Kokoro também falhou: registra e retorna — a resposta textual
                // e o Agent Loop continuam normalmente (fala é opcional).
                AuraLog.Warning("TTS indisponível (Kokoro): " + ex.Message);
            }
        }

        public Task StopAsync()
        {
            _android.StopAsync();
            _kokoro.StopAsync();
            return Task.CompletedTask;
        }
    }
}
