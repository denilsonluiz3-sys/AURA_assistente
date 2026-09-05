using AURA.Mobile.Pages;

namespace AURA.Mobile.Services;

/// <summary>
/// Ponte mínima entre Web AI (AgentPage) e BrowserPage.
/// Não substitui o WebView — só navega e entrega URL pendente.
/// </summary>
public static class AuraBrowserBridge
{
    private static string? _pendingUrl;

    public static void SetPendingUrl(string? url)
    {
        _pendingUrl = string.IsNullOrWhiteSpace(url) ? null : url.Trim();
    }

    public static string? TakePendingUrl()
    {
        string? u = _pendingUrl;
        _pendingUrl = null;
        return u;
    }

    public static async Task OpenInAppBrowserAsync(string url, Page? fromPage = null)
    {
        if (string.IsNullOrWhiteSpace(url))
            return;

        if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            url = "https://" + url.Trim();

        SetPendingUrl(url);

        try
        {
            BrowserPage? browser = ResolveBrowserPage();
            if (browser != null)
                browser.OpenFromBridge(url);

            MainPage? main = ResolveMainPage();
            if (main != null)
            {
                await main.NavigateToProcessAsync("Navegador");
                return;
            }

            if (fromPage?.Navigation != null && browser != null && browser.Parent == null)
            {
                await fromPage.Navigation.PushAsync(browser);
                return;
            }

            await Microsoft.Maui.ApplicationModel.Browser.Default.OpenAsync(
                new Uri(url), Microsoft.Maui.ApplicationModel.BrowserLaunchMode.External);
        }
        catch (Exception ex)
        {
            AuraLog.Exception("AuraBrowserBridge.Open", ex);
            try
            {
                await Microsoft.Maui.ApplicationModel.Browser.Default.OpenAsync(
                    new Uri(url), Microsoft.Maui.ApplicationModel.BrowserLaunchMode.External);
            }
            catch (Exception ex2)
            {
                AuraLog.Exception("AuraBrowserBridge.External", ex2);
            }
        }
    }

    private static BrowserPage? ResolveBrowserPage()
    {
        try
        {
            return Application.Current?.Handler?.MauiContext?.Services.GetService<BrowserPage>();
        }
        catch
        {
            return null;
        }
    }

    private static MainPage? ResolveMainPage()
    {
        try
        {
            if (Application.Current?.Windows?.Count > 0)
            {
                var root = Application.Current.Windows[0].Page;
                if (root is MainPage mp) return mp;
                if (root is NavigationPage nav && nav.RootPage is MainPage mp2) return mp2;
            }
        }
        catch { /* ignore */ }
        return null;
    }
}
