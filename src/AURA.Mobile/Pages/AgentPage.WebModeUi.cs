namespace AURA.Mobile.Pages;

public partial class AgentPage
{
    private void OnModeAgentUiClicked(object sender, EventArgs e)
    {
        _webMode = false;
        StatusBarHost.IsVisible = true;
        InputBarHost.IsVisible = true;
        ApplyModeUi();
    }

    private void OnModeWebUiClicked(object sender, EventArgs e)
    {
        _webMode = true;
        StatusBarHost.IsVisible = false;
        InputBarHost.IsVisible = false;
        ApplyModeUi();

        if (!_webLoaded)
        {
            BridgeWebView.Source = UrlDeepSeek;
            _webLoaded = true;
        }
    }
}
