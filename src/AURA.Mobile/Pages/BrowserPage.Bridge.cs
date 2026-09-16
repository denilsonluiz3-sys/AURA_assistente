using AURA.Core.Security;

namespace AURA.Mobile.Pages;

/// <summary>
/// Entrada da ponte Web AI → Navegador. Partial — não reescreve BrowserPage.xaml.cs.
/// </summary>
public partial class BrowserPage
{
    /// <summary>Abre URL numa aba (nova ou ativa). Seguro para chamar de outra página.</summary>
    public void OpenFromBridge(string url)
    {
        if (!WebSecurityPolicy.TryValidateHttpUrl(url, out Uri validated))
            return;
        url = validated.AbsoluteUri;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                if (!_initialized)
                {
                    _initialized = true;
                    NewTab(url);
                    ApplySettings();
                    return;
                }

                NewTab(url);
            }
            catch (Exception ex)
            {
                AuraLog.Exception("BrowserPage.OpenFromBridge", ex);
            }
        });
    }

    /// <summary>Consome URL pendente do bridge (se OnAppearing já inicializou com home).</summary>
    internal void ConsumePendingBridgeUrl()
    {
        string? pending = AURA.Mobile.Services.AuraBrowserBridge.TakePendingUrl();
        if (string.IsNullOrWhiteSpace(pending))
            return;
        OpenFromBridge(pending);
    }
}
