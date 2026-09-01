using AURA.AI.UniversalAI;
using AURA.Mobile.Diagnostics;

namespace AURA.Mobile.Controls;

/// <summary>
/// Config mínima: preset → key → modelo → Conectar.
/// DeepSeek e OpenRouter com free prioritários.
/// </summary>
public sealed class AiConfigView : ContentView
{
    private sealed record Preset(
        string Id,
        string Label,
        string BaseUrl,
        string ModelsUrl,
        UniversalApiFormat Format,
        bool RequiresKey,
        string ModelHint);

    private static readonly Preset[] Presets =
    {
        new("openrouter", "OpenRouter (free)",
            "https://openrouter.ai/api/v1/chat/completions",
            "https://openrouter.ai/api/v1/models",
            UniversalApiFormat.OpenAiCompatible, true, "deepseek/deepseek-r1:free"),
        new("deepseek", "DeepSeek",
            "https://api.deepseek.com/v1/chat/completions",
            "https://api.deepseek.com/v1/models",
            UniversalApiFormat.OpenAiCompatible, true, "deepseek-chat"),
        new("openai", "OpenAI",
            "https://api.openai.com/v1/chat/completions",
            "https://api.openai.com/v1/models",
            UniversalApiFormat.OpenAiCompatible, true, "gpt-4o-mini"),
        new("ollama", "Ollama (local)",
            "http://127.0.0.1:11434/v1/chat/completions",
            "http://127.0.0.1:11434/v1/models",
            UniversalApiFormat.OpenAiCompatible, false, "llama3.2"),
        new("custom", "Personalizado", "", "", UniversalApiFormat.OpenAiCompatible, true, "")
    };

    private readonly Picker _presetPicker = new() { Title = "Provedor" };
    private readonly Entry _apiKeyEntry = new()
    {
        Placeholder = "API key (DeepSeek: sk-… · OpenRouter: sk-or-…)",
        IsPassword = true,
        ClearButtonVisibility = ClearButtonVisibility.WhileEditing,
        FontSize = 13
    };
    private readonly Entry _modelEntry = new()
    {
        Placeholder = "Modelo (ex.: deepseek-chat ou nome:free)",
        FontSize = 13
    };
    private readonly Picker _modelPicker = new()
    {
        Title = "Modelos (free primeiro)",
        IsVisible = false,
        FontSize = 13
    };
    private readonly Entry _baseUrlEntry = new()
    {
        Placeholder = "URL chat/completions",
        FontSize = 12,
        IsVisible = false
    };
    private readonly Entry _modelsUrlEntry = new()
    {
        Placeholder = "URL /models (opcional)",
        FontSize = 12,
        IsVisible = false
    };
    private readonly Button _advancedToggle = new()
    {
        Text = "▸ Avançado",
        FontSize = 11,
        BackgroundColor = Colors.Transparent,
        TextColor = Color.FromArgb("#8a9bb8"),
        Padding = new Thickness(0, 2),
        HorizontalOptions = LayoutOptions.Start
    };
    private readonly Button _loadModelsButton = new()
    {
        Text = "Listar modelos",
        FontSize = 12,
        HeightRequest = 36
    };
    private readonly Button _connectButton = new()
    {
        Text = "Conectar",
        FontSize = 14,
        FontAttributes = FontAttributes.Bold,
        HorizontalOptions = LayoutOptions.Fill,
        HeightRequest = 40
    };
    private readonly Label _status = new()
    {
        FontSize = 11,
        LineBreakMode = LineBreakMode.WordWrap,
        MaxLines = 4
    };
    private readonly List<string> _models = new();
    private bool _advancedOpen;
    private bool _busy;

    public AiConfigView()
    {
        _presetPicker.ItemsSource = Presets.Select(p => p.Label).ToArray();
        _presetPicker.SelectedIndexChanged += OnPresetChanged;
        _modelPicker.ItemsSource = _models;
        _modelPicker.SelectedIndexChanged += (_, _) =>
        {
            if (_modelPicker.SelectedItem is string m)
            {
                // remover sufixo visual " (free)" se o usuário escolheu display
                var id = m.Replace(" (free)", "", StringComparison.OrdinalIgnoreCase).Trim();
                _modelEntry.Text = id;
            }
        };
        _advancedToggle.Clicked += OnAdvancedToggle;
        _loadModelsButton.Clicked += OnLoadModelsClicked;
        _connectButton.Clicked += OnConnectClicked;
        Loaded += (_, _) => LoadExisting();

        Content = new VerticalStackLayout
        {
            Padding = new Thickness(12, 8),
            Spacing = 6,
            Children =
            {
                new Label
                {
                    Text = "Conectar IA",
                    FontSize = 14,
                    FontAttributes = FontAttributes.Bold
                },
                _presetPicker,
                _apiKeyEntry,
                _modelEntry,
                _modelPicker,
                _loadModelsButton,
                _advancedToggle,
                _baseUrlEntry,
                _modelsUrlEntry,
                _connectButton,
                _status
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

    private Preset? SelectedPreset()
    {
        var i = _presetPicker.SelectedIndex;
        if (i < 0 || i >= Presets.Length)
            return null;
        return Presets[i];
    }

    private void LoadExisting()
    {
        var provider = RuntimeConfig.Provider?.Trim() ?? string.Empty;
        var idx = Array.FindIndex(Presets, p =>
            !string.IsNullOrEmpty(provider) &&
            string.Equals(p.Id, provider, StringComparison.OrdinalIgnoreCase));

        if (idx < 0 && !string.IsNullOrEmpty(RuntimeConfig.BaseUrlOverride))
        {
            idx = Array.FindIndex(Presets, p =>
                !string.IsNullOrEmpty(p.BaseUrl) &&
                RuntimeConfig.BaseUrlOverride.Contains(
                    new Uri(p.BaseUrl).Host,
                    StringComparison.OrdinalIgnoreCase));
        }

        if (idx < 0)
            idx = string.IsNullOrEmpty(provider) && string.IsNullOrEmpty(RuntimeConfig.BaseUrlOverride)
                ? 0
                : Presets.Length - 1;

        _presetPicker.SelectedIndex = idx;
        ApplyPresetFields(Presets[idx], keepExistingUrls: true);

        _apiKeyEntry.Text = RuntimeConfig.GetApiKeyForProvider(
            string.IsNullOrEmpty(RuntimeConfig.Provider) ? Presets[idx].Id : RuntimeConfig.Provider);
        _modelEntry.Text = string.IsNullOrWhiteSpace(RuntimeConfig.Model)
            ? Presets[idx].ModelHint
            : RuntimeConfig.Model;

        _apiKeyEntry.IsVisible = Presets[idx].RequiresKey || Presets[idx].Id == "custom";

        // Semente de modelos free / deepseek offline
        SeedFallbackModels(Presets[idx].Id);
        RefreshStatusLine();
    }

    private void OnPresetChanged(object? sender, EventArgs e)
    {
        var p = SelectedPreset();
        if (p == null)
            return;

        ApplyPresetFields(p, keepExistingUrls: false);
        _apiKeyEntry.IsVisible = p.RequiresKey || p.Id == "custom";
        _modelEntry.Text = p.ModelHint;

        if (p.Id == "custom")
            SetAdvanced(true);

        SeedFallbackModels(p.Id);
        _status.Text = string.Empty;
    }

    private void ApplyPresetFields(Preset p, bool keepExistingUrls)
    {
        if (p.Id == "custom")
        {
            if (keepExistingUrls)
            {
                _baseUrlEntry.Text = RuntimeConfig.BaseUrlOverride;
                _modelsUrlEntry.Text = RuntimeConfig.ModelsUrlOverride;
            }
            return;
        }

        if (!keepExistingUrls || string.IsNullOrWhiteSpace(RuntimeConfig.BaseUrlOverride))
            _baseUrlEntry.Text = p.BaseUrl;
        else
            _baseUrlEntry.Text = RuntimeConfig.BaseUrlOverride;

        if (!keepExistingUrls || string.IsNullOrWhiteSpace(RuntimeConfig.ModelsUrlOverride))
            _modelsUrlEntry.Text = p.ModelsUrl;
        else
            _modelsUrlEntry.Text = RuntimeConfig.ModelsUrlOverride;
    }

    private void SeedFallbackModels(string providerId)
    {
        var fb = UniversalModelDiscovery.FallbackSuggestions(providerId);
        if (fb.Count == 0)
            return;
        ApplyModelList(fb.Select(m => m.Id).ToList());
    }

    private void ApplyModelList(IList<string> ids)
    {
        _models.Clear();
        foreach (var id in ids.Distinct(StringComparer.OrdinalIgnoreCase))
            _models.Add(id);
        _modelPicker.ItemsSource = null;
        _modelPicker.ItemsSource = _models.ToList();
        _modelPicker.IsVisible = _models.Count > 0;
    }

    private void OnAdvancedToggle(object? sender, EventArgs e)
        => SetAdvanced(!_advancedOpen);

    private void SetAdvanced(bool open)
    {
        _advancedOpen = open;
        _baseUrlEntry.IsVisible = open;
        _modelsUrlEntry.IsVisible = open;
        _advancedToggle.Text = open ? "▾ Avançado" : "▸ Avançado";
    }

    private async void OnLoadModelsClicked(object? sender, EventArgs e)
    {
        if (_busy) return;
        _busy = true;
        _loadModelsButton.IsEnabled = false;
        try
        {
            SetStatus("Carregando modelos…", true);
            var count = await LoadModelsFromApiAsync();
            if (count > 0)
                SetStatus($"{count} modelo(s) · free no topo da lista.", true);
            else
                SetStatus("Lista da API vazia — usando sugestões free/offline.", true);
        }
        catch (Exception ex)
        {
            var p = SelectedPreset();
            SeedFallbackModels(p?.Id ?? "openrouter");
            SetStatus("API models falhou — sugestões offline. " + Short(ex.Message, 80), false);
        }
        finally
        {
            _busy = false;
            _loadModelsButton.IsEnabled = true;
        }
    }

    private async Task<int> LoadModelsFromApiAsync()
    {
        var preset = SelectedPreset() ?? Presets[0];
        var modelsUrl = EndpointValidator.Normalize(
            string.IsNullOrWhiteSpace(_modelsUrlEntry.Text) ? preset.ModelsUrl : _modelsUrlEntry.Text);
        var baseUrl = EndpointValidator.Normalize(
            string.IsNullOrWhiteSpace(_baseUrlEntry.Text) ? preset.BaseUrl : _baseUrlEntry.Text);
        var key = ApiKeyValidator.Normalize(_apiKeyEntry.Text);
        var provider = preset.Id;

        if (string.IsNullOrWhiteSpace(modelsUrl))
        {
            SeedFallbackModels(provider);
            return _models.Count;
        }

        var p = UniversalProviderRegistry.Custom(
            provider, baseUrl, modelsUrl, preset.Format,
            RuntimeConfig.AuthHeader, RuntimeConfig.AuthScheme, RuntimeConfig.RequiresApiKey);

        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(25) };
        IReadOnlyList<UniversalModel> list;
        try
        {
            list = await new UniversalModelDiscovery(http).LoadAsync(p, key);
        }
        catch
        {
            list = UniversalModelDiscovery.FallbackSuggestions(provider);
        }

        list = UniversalModelDiscovery.PrioritizeFree(list, max: 200);
        if (list.Count == 0)
            list = UniversalModelDiscovery.FallbackSuggestions(provider);

        ApplyModelList(list.Select(x => x.Id).ToList());
        return _models.Count;
    }

    private async void OnConnectClicked(object? sender, EventArgs e)
    {
        if (_busy)
            return;

        _busy = true;
        _connectButton.IsEnabled = false;
        try
        {
            var preset = SelectedPreset() ?? Presets[^1];
            var provider = preset.Id == "custom"
                ? (string.IsNullOrWhiteSpace(RuntimeConfig.Provider) ? "custom" : RuntimeConfig.Provider.Trim())
                : preset.Id;

            if (preset.Id == "custom" && _presetPicker.SelectedIndex == Presets.Length - 1)
            {
                var host = HostHint(_baseUrlEntry.Text);
                if (!string.IsNullOrEmpty(host))
                    provider = host;
            }

            // DeepSeek: sempre forçar URLs oficiais se o usuário não abriu Avançado com override
            if (preset.Id == "deepseek" && !_advancedOpen)
            {
                _baseUrlEntry.Text = preset.BaseUrl;
                _modelsUrlEntry.Text = preset.ModelsUrl;
            }

            var baseUrl = EndpointValidator.Normalize(
                string.IsNullOrWhiteSpace(_baseUrlEntry.Text) ? preset.BaseUrl : _baseUrlEntry.Text);
            var modelsUrl = EndpointValidator.Normalize(
                string.IsNullOrWhiteSpace(_modelsUrlEntry.Text) ? preset.ModelsUrl : _modelsUrlEntry.Text);
            var model = (_modelEntry.Text ?? string.Empty).Trim()
                .Replace(" (free)", "", StringComparison.OrdinalIgnoreCase);
            var key = ApiKeyValidator.Normalize(_apiKeyEntry.Text);

            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                SetStatus("Informe o endpoint (Avançado) ou escolha um provedor.", false);
                SetAdvanced(true);
                return;
            }

            var endpointError = EndpointValidator.ValidateFormat(baseUrl);
            if (endpointError != null)
            {
                SetStatus(endpointError, false);
                SetAdvanced(true);
                return;
            }

            if (string.IsNullOrWhiteSpace(model))
            {
                model = preset.ModelHint;
                _modelEntry.Text = model;
            }

            if (string.IsNullOrWhiteSpace(model))
            {
                SetStatus("Informe o modelo.", false);
                return;
            }

            // DeepSeek: modelo deve ser deepseek-chat ou deepseek-reasoner (não openrouter/...)
            if (preset.Id == "deepseek" && model.Contains('/', StringComparison.Ordinal))
            {
                SetStatus("DeepSeek API direta: use deepseek-chat ou deepseek-reasoner (não IDs OpenRouter).", false);
                _modelEntry.Text = "deepseek-chat";
                return;
            }

            // OpenRouter free: lembrar sufixo :free se o usuário escolheu deepseek sem provider
            if (preset.Id == "openrouter" &&
                model.StartsWith("deepseek-", StringComparison.OrdinalIgnoreCase) &&
                !model.Contains('/'))
            {
                SetStatus("No OpenRouter use IDs tipo deepseek/deepseek-r1:free (ou Listar modelos).", false);
                return;
            }

            if (preset.Id != "ollama")
            {
                var keyCheck = ApiKeyValidator.ValidateFormat(key, provider, required: true);
                if (!keyCheck.Success)
                {
                    SetStatus(keyCheck.Message, false);
                    return;
                }
            }

            SetStatus("Conectando…", true);

            RuntimeConfig.Provider = provider;
            RuntimeConfig.BaseUrlOverride = baseUrl;
            RuntimeConfig.ModelsUrlOverride = modelsUrl;
            RuntimeConfig.ApiFormat = preset.Format;
            RuntimeConfig.RequiresApiKey = preset.Id != "ollama";
            RuntimeConfig.Model = model;
            RuntimeConfig.SetApiKeyForProvider(provider, key);

            var client = Handler?.MauiContext?.Services.GetService(typeof(IUniversalAiClient)) as IUniversalAiClient;
            if (client != null)
                RuntimeConfig.Apply(client);

            if (preset.Id != "ollama" && !string.IsNullOrEmpty(key))
            {
                var live = await ApiKeyValidator.VerifyLiveAsync(
                    key, baseUrl, model, preset.Format,
                    RuntimeConfig.AuthHeader, RuntimeConfig.AuthScheme,
                    provider, timeoutSeconds: 25);

                if (!live.Success)
                {
                    // Ainda grava a config — usuário pode testar depois; mostra erro claro
                    SetStatus(live.Message + " (config salva; corrija key/modelo e tente de novo)", false);
                    // não return duro em 402/rede: config já persistida
                    if (live.Message.Contains("401") || live.Message.Contains("rejeitada"))
                        return;
                }
                else
                {
                    SetStatus(Short(live.Message, 120), true);
                }
            }
            else
            {
                var probe = await EndpointValidator.ProbeAsync(baseUrl, key, timeoutSeconds: 12);
                SetStatus(probe.Success
                    ? "Conectado · " + Short(probe.Message, 80)
                    : probe.Message, probe.Success);
            }

            // Modelos: free no topo
            try
            {
                await LoadModelsFromApiAsync();
            }
            catch
            {
                SeedFallbackModels(provider);
            }

            RefreshStatusLine();
            if (_status.TextColor != Color.FromArgb("#e2555c"))
                SetStatus("Pronto · " + AiStatusText.ForClient(client), true);
        }
        catch (Exception ex)
        {
            SetStatus("Falha: " + ex.Message, false);
        }
        finally
        {
            _busy = false;
            _connectButton.IsEnabled = true;
        }
    }

    private void RefreshStatusLine()
    {
        var client = Handler?.MauiContext?.Services.GetService(typeof(IUniversalAiClient)) as IUniversalAiClient;
        if (client != null && !string.IsNullOrWhiteSpace(client.Options.BaseUrl))
            _status.Text = AiStatusText.ForClient(client);
    }

    private void SetStatus(string message, bool success)
    {
        _status.Text = message;
        _status.TextColor = success ? Color.FromArgb("#4caf6f") : Color.FromArgb("#e2555c");
    }

    private static string Short(string s, int max)
        => string.IsNullOrEmpty(s) ? s : (s.Length <= max ? s : s[..max] + "…");

    private static string HostHint(string? url)
    {
        try
        {
            var u = EndpointValidator.Normalize(url);
            if (string.IsNullOrEmpty(u)) return string.Empty;
            var host = new Uri(u).Host;
            var dot = host.IndexOf('.');
            return dot > 0 ? host[..dot] : host;
        }
        catch
        {
            return "custom";
        }
    }
}
