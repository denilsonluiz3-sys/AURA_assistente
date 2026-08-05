namespace AURA.Mobile.Pages;

public partial class BrowserPage : ContentPage
{
    private const string HomeUrl = "https://www.google.com";
    private bool _initialized;

    public BrowserPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (!_initialized)
        {
            _initialized = true;
            UrlEntry.Text = HomeUrl;
            await Browser.GoToAsync(HomeUrl);
        }
    }

    private async void OnGoClicked(object sender, EventArgs e)
    {
        string input = UrlEntry.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(input))
        {
            return;
        }

        string url = NormalizeUrl(input);
        await Browser.GoToAsync(url);
    }

    private void OnBackClicked(object sender, EventArgs e)
    {
        if (Browser.CanGoBack)
        {
            Browser.GoBack();
        }
    }

    private void OnForwardClicked(object sender, EventArgs e)
    {
        if (Browser.CanGoForward)
        {
            Browser.GoForward();
        }
    }

    private void OnReloadClicked(object sender, EventArgs e)
    {
        Browser.Reload();
    }

    private void OnNavigating(object sender, WebNavigatingEventArgs e)
    {
        AuraLog.Info("Browser: navegando para " + e.Url);
    }

    private void OnNavigated(object sender, WebNavigatedEventArgs e)
    {
        UrlEntry.Text = e.Url;
        BackButton.IsEnabled = Browser.CanGoBack;
        ForwardButton.IsEnabled = Browser.CanGoForward;
    }

    private static string NormalizeUrl(string input)
    {
        input = input.Trim();

        bool hasScheme = input.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || input.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

        bool looksLikeQuery = input.Contains(' ')
            || (!input.Contains('.') && !hasScheme);

        if (looksLikeQuery)
        {
            return "https://www.google.com/search?q=" + Uri.EscapeDataString(input);
        }

        return hasScheme ? input : "https://" + input;
    }
}
