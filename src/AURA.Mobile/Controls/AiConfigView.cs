using AURA.AI;
using AURA.AI.UniversalAI;
using AURA.Mobile.Diagnostics;

namespace AURA.Mobile.Controls;

/// <summary>Configuração universal: API key -> carregar modelos -> selecionar -> salvar.</summary>
public sealed class AiConfigView : ContentView
{
    private readonly Entry _apiKeyEntry = new() { Placeholder = "API key", IsPassword = true, ClearButtonVisibility = ClearButtonVisibility.WhileEditing };
    private readonly Button _loadModelsButton = new() { Text = "CARREGAR MODELOS", HorizontalOptions = LayoutOptions.Fill };
    private readonly Picker _modelPicker = new() { Title = "Selecione o modelo", IsEnabled = false, HorizontalOptions = LayoutOptions.Fill };
    private readonly Button _saveButton = new() { Text = "SALVAR", IsEnabled = false, HorizontalOptions = LayoutOptions.Fill };
    private readonly Label _status = new() { FontSize = 12, LineBreakMode = LineBreakMode.WordWrap };
    private readonly List<string> _models = new();
    private OpenRouterClient? _client;
    private UniversalProvider? _discoveredProvider;

    public AiConfigView()
    {
        _modelPicker.ItemsSource = _models;
        _loadModelsButton.Clicked += OnLoadModelsClicked;
        _saveButton.Clicked += OnSaveClicked;
        Loaded += (_, _) => LoadExisting();
        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(20, 16), Spacing = 12,
                Children =
                {
                    new Label { Text = "Configuração de IA", FontSize = 24, FontAttributes = FontAttributes.Bold },
                    new Label { Text = "API KEY", FontAttributes = FontAttributes.Bold },
                    _apiKeyEntry,
                    _loadModelsButton,
                    new ActivityIndicator { IsVisible = false },
                    new Label { Text = "MODELO", FontAttributes = FontAttributes.Bold },
                    _modelPicker,
                    _saveButton,
                    _status
                }
            }
        };
    }

    public void Load(OpenRouterClient client)
    {
        _client = client;
        LoadExisting();
    }

    // Preserva o contrato usado por AiConfig/fluxos antigos.
    public void ApplyToClient()
    {
        if (_client == null || _discoveredProvider == null) return;
        var model = _modelPicker.SelectedItem?.ToString();
        if (string.IsNullOrWhiteSpace(model)) return;
        RuntimeConfig.ApplyUniversal(_client, _discoveredProvider.Id, _apiKeyEntry.Text?.Trim() ?? string.Empty, model, _discoveredProvider.BaseUrl, _discoveredProvider.ModelsUrl);
    }

    private void LoadExisting()
    {
        var provider = UniversalProviderRegistry.BuiltIns.FirstOrDefault(p => string.Equals(p.Id, RuntimeConfig.Provider, StringComparison.OrdinalIgnoreCase));
        if (provider == null) return;
        _discoveredProvider = provider;
        _apiKeyEntry.Text = RuntimeConfig.GetApiKeyForProvider(provider.Id);
        if (!string.IsNullOrWhiteSpace(RuntimeConfig.Model))
        {
            _models.Clear(); _models.Add(RuntimeConfig.Model);
            _modelPicker.ItemsSource = null; _modelPicker.ItemsSource = _models;
            _modelPicker.SelectedItem = RuntimeConfig.Model;
            _modelPicker.IsEnabled = true; _saveButton.IsEnabled = !string.IsNullOrWhiteSpace(_apiKeyEntry.Text);
        }
    }

    private async void OnLoadModelsClicked(object? sender, EventArgs e)
    {
        var key = _apiKeyEntry.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(key)) { SetStatus("Informe a API key.", false); return; }
        SetBusy(true);
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(Math.Clamp(RuntimeConfig.TimeoutSeconds, 5, 30)) };
            UniversalProvider? provider = null;
            IReadOnlyList<UniversalModel>? models = null;

            // Primeiro tenta os endpoints conhecidos. O primeiro /models autenticado com sucesso define o provider.
            foreach (var candidate in UniversalProviderRegistry.BuiltIns.Where(p => p.RequiresApiKey))
            {
                try
                {
                    var discovery = new UniversalModelDiscovery(http);
                    var found = await discovery.LoadAsync(candidate, key);
                    if (found.Count > 0) { provider = candidate; models = found; break; }
                }
                catch { }
            }

            if (provider == null || models == null || models.Count == 0)
            {
                SetStatus("Não foi possível carregar modelos. Verifique a chave e o provedor.", false);
                return;
            }

            _discoveredProvider = provider;
            RuntimeConfig.Provider = provider.Id;
            RuntimeConfig.SetApiKeyForProvider(provider.Id, key);
            RuntimeConfig.ApiFormat = provider.Format switch
            {
                UniversalApiFormat.AnthropicMessages => AURA.AI.AiApiFormat.AnthropicMessages,
                UniversalApiFormat.Gemini => AURA.AI.AiApiFormat.GeminiGenerateContent,
                _ => AURA.AI.AiApiFormat.OpenAICompletions
            };

            _models.Clear(); _models.AddRange(models.Select(m => m.Id));
            _modelPicker.ItemsSource = null; _modelPicker.ItemsSource = _models;
            _modelPicker.IsEnabled = true; _modelPicker.SelectedIndex = 0; _saveButton.IsEnabled = true;
            SetStatus($"{provider.Name}: {_models.Count} modelo(s) carregado(s).", true);
        }
        catch (Exception ex)
        {
            SetStatus("Falha ao carregar modelos: " + ex.Message, false);
            AuraLog.Exception("AiConfigView.LoadModels", ex);
        }
        finally { SetBusy(false); }
    }

    private void OnSaveClicked(object? sender, EventArgs e)
    {
        try
        {
            var model = _modelPicker.SelectedItem?.ToString();
            var key = _apiKeyEntry.Text?.Trim();
            if (_discoveredProvider == null || string.IsNullOrWhiteSpace(model) || string.IsNullOrWhiteSpace(key))
            { SetStatus("Carregue os modelos e selecione um modelo.", false); return; }
            RuntimeConfig.ApplyUniversal(_client!, _discoveredProvider.Id, key, model, _discoveredProvider.BaseUrl, _discoveredProvider.ModelsUrl);
            SetStatus("Configuração salva.", true);
        }
        catch (Exception ex) { SetStatus("Falha ao salvar: " + ex.Message, false); }
    }

    private void SetBusy(bool busy)
    {
        _apiKeyEntry.IsEnabled = !busy; _loadModelsButton.IsEnabled = !busy;
        _loadModelsButton.Text = busy ? "CARREGANDO..." : "CARREGAR MODELOS";
    }

    private void SetStatus(string message, bool success)
    {
        _status.Text = message;
        _status.TextColor = success ? Colors.Green : Colors.Red;
    }
}
