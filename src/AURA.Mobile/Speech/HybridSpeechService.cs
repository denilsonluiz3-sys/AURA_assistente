using System;
using System.Threading;
using System.Threading.Tasks;

namespace AURA.Mobile.Speech
{
    /// <summary>
    /// 1) TTS Android nativo (texto livre).
    /// 2) Kokoro só como último recurso — frases fora do dicionário NÃO disparam
    ///    excessão ruidosa: apenas logam e seguem em silêncio (evita “só volume”).
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
            catch (NotSupportedException) { }
            catch (InvalidOperationException) { }
        }

        public async Task SpeakAsync(string text, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            try
            {
                await _android.SpeakAsync(text, ct).ConfigureAwait(false);
                return;
            }
            catch (NotSupportedException) { }
            catch (InvalidOperationException) { }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                AuraLog.Exception("HybridSpeechService.AndroidTts", ex);
            }

            // Kokoro: só frases curtas conhecidas; senão silêncio (não estoura volume)
            try
            {
                await _kokoro.InitializeAsync(ct).ConfigureAwait(false);
                await _kokoro.SpeakAsync(text, ct).ConfigureAwait(false);
            }
            catch (NotSupportedException)
            {
                AuraLog.Info("TTS: texto fora do alcance do motor atual, fala pulada.");
            }
            catch (Exception ex)
            {
                AuraLog.Exception("HybridSpeechService.Kokoro", ex);
            }
        }

        public Task StopAsync()
        {
            try { _android.StopAsync(); } catch { }
            try { _kokoro.StopAsync(); } catch { }
            return Task.CompletedTask;
        }
    }
}
