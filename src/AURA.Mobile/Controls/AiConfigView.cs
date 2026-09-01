using AURA.AI.UniversalAI;
using AURA.Mobile.Diagnostics;

namespace AURA.Mobile.Controls;

/// <summary>Configuração universal: provider, endpoint, autenticação e modelo são definidos pelo usuário.</summary>
public sealed class AiConfigView : ContentView
{
    private readonly Entry _providerEntry = new() { Placeholder = "ex.: openrouter, ollama, deepseek" };
    private readonly Entry _baseUrlEntry = new() { Placeholder = "https://…/api/v1/chat/completions" };
    private readonly Entry _modelsUrlEntry = new() { Placeholder = "Endpoint de modelos (opcional)" };
    private readonly Picker _formatPicker = new() { Title = "Formato da API" };
    private readonly Entry _apiKeyEntry = new() { Placeholder = "API key", IsPassword = true, ClearButtonVisibility = ClearButtonVisibility.WhileEditing };
    private readonly Button _validateKeyButton = new() { Text = "VALIDAR API KEY", HorizontalOptions = LayoutOptions.Fill };
    private readonly Button _validateEndpointButton = new() { Text = "VALIDAR ENDPOINT", HorizontalOptions = LayoutOptions.Fill };
    private readonly Button _loadModelsButton = new() { Text = "CARREGAR MODELOS", HorizontalOptions = LayoutOptions.Fill };
    private readonly Entry _modelEntry = new() { Placeholder = "ID do modelo (ex.: qwen/qwen-plus)" };
    private readonly Picker _modelPicker = new() { Title = "Ou selecione o modelo", IsEnabled = false, HorizontalOptions = LayoutOptions.Fill };
    private readonly Button _saveButton = new() { Text = "SALVAR", IsEnabled = true, HorizontalOptions = LayoutOptions.Fill };
    private readonly Label _status = new() { FontSize = 12, LineBreakMode = LineBreakMode.WordWrap };
    private readonly List<string> _models = new();

    public AiConfigView()
    {
        _formatPicker.ItemsSource = Enum.GetValues<UniversalApiFormat>().Select(x => x.ToString()).ToArray();
        _formatPicker.SelectedItem = UniversalApiFormat.OpenAiCompatible.ToString();
        _modelPicker.ItemsSource = _models;
        _validateKeyButton.Clicked += OnValidateKeyClicked;
        _validateEndpointButton.Clicked += OnValidateEndpointClicked;
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
                    _validateKeyButton,
                    _validateEndpointButton,
                    _loadModelsButton,
                    new Label { Text = "MODELO" }, _modelEntry, _modelPicker,
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
        _modelEntry.Text = RuntimeConfig.Model;
        if (!string.IsNullOrWhiteSpace(RuntimeConfig.Model))
        {
            _models.Clear();
            _models.Add(RuntimeConfig.Model);
            _modelPicker.ItemsSource = null;
            _modelPicker.ItemsSource = _models;
            _modelPicker.SelectedIndex = 0;
            _modelPicker.IsEnabled = true;
        }
        _saveButton.IsEnabled = true;
    }

    private UniversalApiFormat CurrentApiFormat()
        => Enum.TryParse<UniversalApiFormat>(_formatPicker.SelectedItem?.ToString(), out var f)
            ? f
            : UniversalApiFormat.OpenAiCompatible;

    private UniversalApiFormatHint CurrentFormatHint()
        => CurrentApiFormat() switch
        {
            UniversalApiFormat.AnthropicMessages => UniversalApiFormatHint.AnthropicMessages,
            UniversalApiFormat.Gemini => UniversalApiFormatHint.Gemini,
            _ => UniversalApiFormatHint.OpenAiCompatible
        };

    private string ResolveModel()
    {
        var model = _modelPicker.SelectedItem?.ToString();
        if (string.IsNullOrWhiteSpace(model))
            model = _modelEntry.Text?.Trim();
        if (string.IsNullOrWhiteSpace(model))
            model = RuntimeConfig.Model;
        return model?.Trim() ?? string.Empty;
    }

    private void ShowValidation(ApiKeyValidationResult result)
    {
        SetStatus(result.Message, result.Success && !result.IsWarning);
        if (result.IsWarning)
            _status.TextColor = Colors.Orange;
    }

    private async void OnValidateKeyClicked(object? sender, EventArgs e)
    {
        _validateKeyButton.IsEnabled = false;
        try
        {
            var provider = _providerEntry.Text?.Trim();
            var key = ApiKeyValidator.Normalize(_apiKeyEntry.Text);
            _apiKeyEntry.Text = key;

            var formatOnly = ApiKeyValidator.ValidateFormat(key, provider, required: RuntimeConfig.RequiresApiKey);
            if (!formatOnly.Success)
            {
                ShowValidation(formatOnly);
                return;
            }

            var baseUrl = EndpointValidator.Normalize(_baseUrlEntry.Text);
            _baseUrlEntry.Text = baseUrl;
            var model = ResolveModel();

            if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(model))
            {
                // Só formato — sem endpoint/modelo não dá para probe ao vivo
                ShowValidation(formatOnly.IsWarning
                    ? formatOnly
                    : ApiKeyValidationResult.Warn(
                        formatOnly.Message + " Informe endpoint e modelo para testar a key no provider.", key));
                return;
            }

            SetStatus("Verificando API key no endpoint…", true);
            var live = await ApiKeyValidator.VerifyLiveAsync(
                key,
                baseUrl,
                model,
                CurrentApiFormat(),
                RuntimeConfig.AuthHeader,
                RuntimeConfig.AuthScheme,
                provider,
                Math.Clamp(RuntimeConfig.TimeoutSeconds, 5, 30));

            ShowValidation(live);
        }
        catch (Exception ex)
        {
            SetStatus("Falha ao validar key: " + ex.Message, false);
        }
        finally
        {
            _validateKeyButton.IsEnabled = true;
        }
    }

    private async void OnValidateEndpointClicked(object? sender, EventArgs e)
    {
        _validateEndpointButton.IsEnabled = false;
        try
        {
            var baseUrl = EndpointValidator.Normalize(_baseUrlEntry.Text);
            _baseUrlEntry.Text = baseUrl;

            var formatError = EndpointValidator.ValidateFormat(baseUrl, CurrentFormatHint());
            if (formatError != null)
            {
                SetStatus(formatError, false);
                return;
            }

            SetStatus("Validando endpoint…", true);
            var key = ApiKeyValidator.Normalize(_apiKeyEntry.Text);
            var result = await EndpointValidator.ProbeAsync(
                baseUrl,
                key,
                RuntimeConfig.AuthHeader,
                RuntimeConfig.AuthScheme,
                Math.Clamp(RuntimeConfig.TimeoutSeconds, 5, 30));

            SetStatus(result.Message, result.Success && !result.IsWarning);
            if (result.IsWarning)
                _status.TextColor = Colors.Orange;
        }
        catch (Exception ex)
        {
            SetStatus("Falha na validação: " + ex.Message, false);
        }
        finally
        {
            _validateEndpointButton.IsEnabled = true;
        }
    }

    private async void OnLoadModelsClicked(object? sender, EventArgs e)
    {
        try
        {
            var provider = _providerEntry.Text?.Trim();
            var baseUrl = EndpointValidator.Normalize(_baseUrlEntry.Text);
            _baseUrlEntry.Text = baseUrl;
            var modelsUrl = EndpointValidator.Normalize(_modelsUrlEntry.Text);
            _modelsUrlEntry.Text = modelsUrl;

            if (string.IsNullOrWhiteSpace(provider) || string.IsNullOrWhiteSpace(baseUrl))
            {
                SetStatus("Informe provider e endpoint.", false);
                return;
            }

            var formatError = EndpointValidator.ValidateFormat(baseUrl, CurrentFormatHint());
            if (formatError != null)
            {
                SetStatus(formatError, false);
                return;
            }

            var keyNorm = ApiKeyValidator.Normalize(_apiKeyEntry.Text);
            _apiKeyEntry.Text = keyNorm;
            RuntimeConfig.SetApiKeyForProvider(provider, keyNorm);

            var format = CurrentApiFormat();
            var p = UniversalProviderRegistry.Custom(
                provider, baseUrl, modelsUrl, format,
                RuntimeConfig.AuthHeader, RuntimeConfig.AuthScheme, RuntimeConfig.RequiresApiKey);
            var key = RuntimeConfig.GetApiKeyForProvider(provider);
            using var http = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(Math.Clamp(RuntimeConfig.TimeoutSeconds, 5, 120))
            };

            if (string.IsNullOrWhiteSpace(p.ModelsUrl))
            {
                SetStatus("Sem URL de modelos — digite o modelo manualmente e toque SALVAR.", false);
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
            {
                _modelPicker.SelectedIndex = 0;
                _modelEntry.Text = _models[0];
            }
            _saveButton.IsEnabled = true;
            SetStatus($"{_models.Count} modelo(s). Key {(string.IsNullOrEmpty(key) ? "ausente" : "ok")}.", true);
        }
        catch (Exception ex)
        {
            SetStatus("Falha ao carregar modelos: " + ex.Message, false);
            _saveButton.IsEnabled = true;
        }
    }

    private void OnSaveClicked(object? sender, EventArgs e)
    {
        try
        {
            var provider = _providerEntry.Text?.Trim();
            var baseUrl = EndpointValidator.Normalize(_baseUrlEntry.Text);
            _baseUrlEntry.Text = baseUrl;
            var model = ResolveModel();

            if (string.IsNullOrWhiteSpace(provider) || string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(model))
            {
                SetStatus("Provider, endpoint e modelo são obrigatórios.", false);
                return;
            }

            var endpointError = EndpointValidator.ValidateFormat(baseUrl, CurrentFormatHint());
            if (endpointError != null)
            {
                SetStatus(endpointError, false);
                return;
            }

            var key = ApiKeyValidator.Normalize(_apiKeyEntry.Text);
            _apiKeyEntry.Text = key;

            var keyCheck = ApiKeyValidator.ValidateFormat(key, provider, required: RuntimeConfig.RequiresApiKey);
            if (!keyCheck.Success)
            {
                ShowValidation(keyCheck);
                return;
            }

            RuntimeConfig.Provider = provider;
            RuntimeConfig.BaseUrlOverride = baseUrl;
            RuntimeConfig.ModelsUrlOverride = EndpointValidator.Normalize(_modelsUrlEntry.Text);
            RuntimeConfig.ApiFormat = CurrentApiFormat();
            RuntimeConfig.Model = model;
            RuntimeConfig.SetApiKeyForProvider(provider, key);

            var stored = RuntimeConfig.GetApiKeyForProvider(provider);
            var keyOk = string.IsNullOrWhiteSpace(key)
                        || string.Equals(stored, key, StringComparison.Ordinal);

            var client = Handler?.MauiContext?.Services.GetService(typeof(IUniversalAiClient)) as IUniversalAiClient;
            if (client != null)
                RuntimeConfig.Apply(client);

            var clientHasKey = client != null && !string.IsNullOrWhiteSpace(client.Options.ApiKey);

            if (!string.IsNullOrWhiteSpace(key) && !keyOk)
            {
                SetStatus("Falha: a API key não foi persistida. Tente de novo.", false);
                return;
            }

            if (!string.IsNullOrWhiteSpace(key) && !clientHasKey)
            {
                SetStatus("Key gravada, mas o cliente ainda está sem key — reabra o Agente.", false);
                return;
            }

            var msg = string.IsNullOrWhiteSpace(key)
                ? "Configuração salva (sem API key)."
                : "Configuração e API key salvas.";
            if (keyCheck.IsWarning)
                msg += " Aviso: " + keyCheck.Message;

            SetStatus(msg, true);
            if (keyCheck.IsWarning)
                _status.TextColor = Colors.Orange;
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
