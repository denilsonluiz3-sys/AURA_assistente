using AURA.AI.UniversalAI;
using AURA.Mobile.Diagnostics;

namespace AURA.Mobile.Controls;

/// <summary>Configuração universal: provider, endpoint, autenticação e modelo são definidos pelo usuário.</summary>
public sealed class AiConfigView : ContentView
{
    private readonly Entry _providerEntry = new() { Placeholder = "Identificador do provider" };
    private readonly Entry _baseUrlEntry = new() { Placeholder = "Endpoint de chat" };
    private readonly Entry _modelsUrlEntry = new() { Placeholder = "Endpoint de modelos (opcional)" };
    private readonly Picker _formatPicker = new() { Title = "Formato da API" };
    private readonly Entry _apiKeyEntry = new() { Placeholder = "API key (opcional)", IsPassword = true, ClearButtonVisibility = ClearButtonVisibility.WhileEditing };
    private readonly Button _loadModelsButton = new() { Text = "CARREGAR MODELOS", HorizontalOptions = LayoutOptions.Fill };
    private readonly Picker _modelPicker = new() { Title = "Selecione o modelo", IsEnabled = false, HorizontalOptions = LayoutOptions.Fill };
    private readonly Button _saveButton = new() { Text = "SALVAR", IsEnabled = false, HorizontalOptions = LayoutOptions.Fill };
    private readonly Label _status = new() { FontSize = 12, LineBreakMode = LineBreakMode.WordWrap };
    private readonly List<string> _models = new();

    public AiConfigView()
    {
        _formatPicker.ItemsSource = Enum.GetValues<UniversalApiFormat>().Select(x => x.ToString()).ToArray();
        _modelPicker.ItemsSource = _models;
        _loadModelsButton.Clicked += OnLoadModelsClicked;
        _saveButton.Clicked += OnSaveClicked;
        Loaded += (_, _) => LoadExisting();
        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(20, 16),
                Spacing = 12,
                Children =
                {
                    new Label { Text = "Configuração de IA", FontSize = 24, FontAttributes = FontAttributes.Bold },
                    new Label { Text = "PROVIDER" }, _providerEntry,
                    new Label { Text = "ENDPOINT" }, _baseUrlEntry,
                    new Label { Text = "MODELOS (opcional)" }, _modelsUrlEntry,
                    new Label { Text = "FORMATO" }, _formatPicker,
                    new Label { Text = "API KEY" }, _apiKeyEntry,
                    _loadModelsButton,
                    new Label { Text = "MODELO" }, _modelPicker,
                    _saveButton,
                    _status
                }
            }
        };
    }

    public void Load(IUniversalAiClient? client = null)
    {
        LoadExisting();
        if (client != null)
            RuntimeConfig.Apply(client);
    }

    public void LoadExistingConfiguration() => LoadExisting();

    public void ApplyToClient()
    {
        var client = Handler?.MauiContext?.Services.GetService(typeof(IUniversalAiClient)) as IUniversalAiClient;
        if (client != null)
            RuntimeConfig.Apply(client);
    }

    private void LoadExisting()
    {
        _providerEntry.Text = RuntimeConfig.Provider;
        _baseUrlEntry.Text = RuntimeConfig.BaseUrlOverride;
        _modelsUrlEntry.Text = RuntimeConfig.ModelsUrlOverride;
        _apiKeyEntry.Text = RuntimeConfig.GetApiKeyForProvider(RuntimeConfig.Provider);
        _formatPicker.SelectedItem = RuntimeConfig.ApiFormat.ToString();
        if (!string.IsNullOrWhiteSpace(RuntimeConfig.Model))
        {
            _models.Clear();
            _models.Add(RuntimeConfig.Model);
            _modelPicker.ItemsSource = null;
            _modelPicker.ItemsSource = _models;
            _modelPicker.SelectedIndex = 0;
            _modelPicker.IsEnabled = true;
            _saveButton.IsEnabled = true;
        }
    }

    private async void OnLoadModelsClicked(object? sender, EventArgs e)
    {
        try
        {
            var provider = _providerEntry.Text?.Trim();
            var baseUrl = _baseUrlEntry.Text?.Trim();
            var modelsUrl = _modelsUrlEntry.Text?.Trim();
            if (string.IsNullOrWhiteSpace(provider) || string.IsNullOrWhiteSpace(baseUrl))
            {
                SetStatus("Informe provider e endpoint.", false);
                return;
            }

            var format = Enum.TryParse<UniversalApiFormat>(_formatPicker.SelectedItem?.ToString(), out var f)
                ? f
                : UniversalApiFormat.OpenAiCompatible;
            var p = UniversalProviderRegistry.Custom(
                provider, baseUrl, modelsUrl, format,
                RuntimeConfig.AuthHeader, RuntimeConfig.AuthScheme, RuntimeConfig.RequiresApiKey);
            var key = _apiKeyEntry.Text?.Trim() ?? string.Empty;
            using var http = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(Math.Clamp(RuntimeConfig.TimeoutSeconds, 5, 120))
            };

            if (string.IsNullOrWhiteSpace(p.ModelsUrl))
            {
                SetStatus("Endpoint de modelos não informado; selecione um modelo manualmente antes de salvar.", false);
                _modelPicker.IsEnabled = true;
                _saveButton.IsEnabled = true;
                return;
            }

            var models = await new UniversalModelDiscovery(http).LoadAsync(p, key);
            _models.Clear();
            _models.AddRange(models.Select(x => x.Id));
            _modelPicker.ItemsSource = null;
            _modelPicker.ItemsSource = _models;
            _modelPicker.IsEnabled = _models.Count > 0;
            if (_models.Count > 0)
                _modelPicker.SelectedIndex = 0;
            _saveButton.IsEnabled = true;
            SetStatus($"{_models.Count} modelo(s) carregado(s).", true);
        }
        catch (Exception ex)
        {
            SetStatus("Falha ao carregar modelos: " + ex.Message, false);
        }
    }

    private void OnSaveClicked(object? sender, EventArgs e)
    {
        try
        {
            var provider = _providerEntry.Text?.Trim();
            var baseUrl = _baseUrlEntry.Text?.Trim();
            var model = _modelPicker.SelectedItem?.ToString() ?? RuntimeConfig.Model;
            if (string.IsNullOrWhiteSpace(provider) || string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(model))
            {
                SetStatus("Provider, endpoint e modelo são obrigatórios.", false);
                return;
            }

            RuntimeConfig.Provider = provider;
            RuntimeConfig.BaseUrlOverride = baseUrl;
            RuntimeConfig.ModelsUrlOverride = _modelsUrlEntry.Text?.Trim() ?? string.Empty;
            RuntimeConfig.ApiFormat = Enum.TryParse<UniversalApiFormat>(_formatPicker.SelectedItem?.ToString(), out var f)
                ? f
                : UniversalApiFormat.OpenAiCompatible;
            RuntimeConfig.SetApiKeyForProvider(provider, _apiKeyEntry.Text);
            RuntimeConfig.Model = model;

            var client = Handler?.MauiContext?.Services.GetService(typeof(IUniversalAiClient)) as IUniversalAiClient;
            if (client != null)
                RuntimeConfig.Apply(client);

            SetStatus("Configuração salva.", true);
        }
        catch (Exception ex)
        {
            SetStatus("Falha ao salvar: " + ex.Message, false);
        }
    }

    private void SetStatus(string message, bool success)
    {
        _status.Text = message;
        _status.TextColor = success ? Colors.Green : Colors.Red;
    }
}
