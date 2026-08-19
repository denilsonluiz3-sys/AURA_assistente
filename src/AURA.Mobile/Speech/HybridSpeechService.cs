using System;
using System.Threading;
using System.Threading.Tasks;

namespace AURA.Mobile.Speech
{
    /// <summary>
    /// Android native TTS service.
    ///
    /// KokoroSpeechService was removed from the mobile project; keep this
    /// service focused on the native Android implementation so the project
    /// does not retain a reference to a deleted type.
    /// </summary>
    public sealed class HybridSpeechService : ISpeechService
    {
        private readonly AndroidTtsSpeechService _android = new();

        public bool IsReady => _android.IsReady;

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
            }
            catch (NotSupportedException) { }
            catch (InvalidOperationException) { }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                AuraLog.Exception("HybridSpeechService.AndroidTts", ex);
            }
        }

        public Task StopAsync()
        {
            try { _android.StopAsync(); } catch { }
            return Task.CompletedTask;
        }
    }
}
