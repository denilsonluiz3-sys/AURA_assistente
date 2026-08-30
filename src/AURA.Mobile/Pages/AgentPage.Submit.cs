using System.Threading;

namespace AURA.Mobile.Pages;

/// <summary>
/// Proteção contra disparo duplo (Editor.Completed + botão ▶) e sessão reutilizável.
/// </summary>
public partial class AgentPage
{
    /// <summary>0 = livre, 1 = em execução. Interlocked para concorrência.</summary>
    private int _runGate;

    /// <summary>Último submit (TickCount64) — debounce teclado Android.</summary>
    private long _lastSubmitTicks;

    /// <summary>Fingerprint do provedor/modelo/baseUrl da sessão atual.</summary>
    private string? _sessionFingerprint;

    /// <summary>ID da execução ativa (só a resposta deste id vira bolha final).</summary>
    private int _activeRequestId;

    private static int _requestSeq;

    /// <summary>
    /// Único ponto de entrada para Editor.Completed, botão ▶ e chips.
    /// </summary>
    private async Task TrySubmitAsync(string source)
    {
        long now = Environment.TickCount64;
        if (_lastSubmitTicks != 0 && now - _lastSubmitTicks < 900)
        {
            AuraLog.Info($"AgentPage.Submit ignorado (debounce source={source})");
            return;
        }

        if (Interlocked.CompareExchange(ref _runGate, 1, 0) != 0)
        {
            AuraLog.Info($"AgentPage.Submit ignorado (em voo source={source})");
            return;
        }

        _lastSubmitTicks = now;
        int requestId = Interlocked.Increment(ref _requestSeq);
        _activeRequestId = requestId;
        AuraLog.Info($"AgentPage.Submit BEGIN id={requestId} source={source}");

        try
        {
            await RunAgentRequestAsync(requestId).ConfigureAwait(true);
        }
        finally
        {
            if (_activeRequestId == requestId)
                _activeRequestId = 0;
            Interlocked.Exchange(ref _runGate, 0);
            AuraLog.Info($"AgentPage.Submit END id={requestId}");

            try
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    try
                    {
                        RunButton.IsEnabled = true;
                        BusyIndicator.IsRunning = false;
                        BusyIndicator.IsVisible = false;
                        CommandEditor.IsEnabled = true;
                    }
                    catch { /* ignore */ }
                });
            }
            catch { /* ignore */ }
        }
    }

    private bool IsCurrentRequest(int requestId) =>
        requestId != 0 && requestId == _activeRequestId;

    private void EnsureSessionForCurrentProvider()
    {
        string fp = ($"{_client.Options.Provider}|{_client.Options.Model}|{_client.Options.BaseUrl}|{_client.Options.ApiKey?.Length ?? 0}").ToLowerInvariant();
        if (_session != null && string.Equals(_sessionFingerprint, fp, StringComparison.Ordinal))
            return;

        _session = null;
        _sessionFingerprint = fp;
        EnsureSession();
    }
}
