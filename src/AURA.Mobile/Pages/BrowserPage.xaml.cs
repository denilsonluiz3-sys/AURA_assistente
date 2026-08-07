using AURA.Mobile.Diagnostics;

namespace AURA.Mobile.Pages
{
    public partial class BrowserPage : ContentPage
    {
        private const string HomeUrl = "https://www.google.com";
        private const string EnginePrefKey = "browser_engine";
        private const string EngineNamePrefKey = "browser_engine_name";

        private readonly List<SearchEngine> _engines = SearchCatalog.Engines;
        private readonly ImageSearchPage _imageSearch;
        private bool _initialized;

        public BrowserPage(ImageSearchPage imageSearch)
        {
            InitializeComponent();
            _imageSearch = imageSearch;

            EnginePicker.ItemsSource = _engines.Select(e => e.Name).ToList();

            int saved = Preferences.Default.Get(EnginePrefKey, -1);
            EnginePicker.SelectedIndex = saved >= 0 && saved < _engines.Count ? saved : 0;
            EnginePicker.SelectedIndexChanged += OnEngineChanged;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            if (!_initialized)
            {
                _initialized = true;
                UrlEntry.Text = HomeUrl;
                Browser.Source = HomeUrl;
            }
        }

        private SearchEngine CurrentEngine =>
            _engines[Math.Max(0, EnginePicker.SelectedIndex)];

        private void OnEngineChanged(object sender, EventArgs e)
        {
            if (EnginePicker.SelectedIndex < 0)
            {
                return;
            }

            Preferences.Default.Set(EnginePrefKey, EnginePicker.SelectedIndex);
            Preferences.Default.Set(EngineNamePrefKey, CurrentEngine.Name);
        }

        private void OnGoClicked(object sender, EventArgs e)
        {
            string input = UrlEntry.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(input))
            {
                return;
            }

            if (IsOnionAddress(input))
            {
                HandleOnion(input);
                return;
            }

            Browser.Source = NormalizeUrl(input);
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

        private async void OnImageSearchClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(_imageSearch);
        }

        private void OnVpnClicked(object sender, EventArgs e)
        {
#if ANDROID
            AURA.Mobile.Platforms.Android.VpnHelper.OpenVpnSettings();
#else
            Browser.Source = "https://www.android.com/vpn/";
#endif
        }

        // --- Onion (.onion) ---

        private static bool IsOnionAddress(string input)
        {
            string lower = input.ToLowerInvariant();
            return lower.Contains(".onion");
        }

        private async void HandleOnion(string input)
        {
#if ANDROID
            bool installed = AURA.Mobile.Platforms.Android.VpnHelper.IsOrbotInstalled();
#else
            bool installed = false;
#endif

            string message = installed
                ? "Endereço .onion exige Tor ativo.\n\nAbra o Orbot e ative o modo VPN (a conexão de todo o aparelho passa pelo Tor — então o WebView consegue acessar .onion). Depois toque em 'Abrir Orbot' e tente de novo."
                : "Endereço .onion exige Tor, que não vem no Android.\n\nInstale o Orbot (Tor oficial): ele oferece modo VPN que roteia o app inteiro pela rede Tor, permitindo abrir .onion no navegador. O WebView da AURA não pode se conectar a .onion sem ele.";

            string action = installed ? "Abrir Orbot" : "Instalar Orbot";

            bool? answer = await DisplayAlertAsync("Tor (.onion)", message, action, "Cancelar");
            if (answer == true)
            {
#if ANDROID
                if (installed)
                {
                    AURA.Mobile.Platforms.Android.VpnHelper.OpenOrbot();
                }
                else
                {
                    Browser.Source = AURA.Mobile.Platforms.Android.VpnHelper.OrbotPlayStoreUrl;
                }
#endif
            }
        }

        // --- Pesquisa / URL ---

        private string NormalizeUrl(string input)
        {
            input = input.Trim();

            bool hasScheme = input.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || input.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

            bool looksLikeQuery = input.Contains(' ')
                || (!input.Contains('.') && !hasScheme);

            if (looksLikeQuery)
            {
                return string.Format(CurrentEngine.SearchUrl, Uri.EscapeDataString(input));
            }

            return hasScheme ? input : "https://" + input;
        }
    }
}
