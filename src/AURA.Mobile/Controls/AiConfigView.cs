using AURA.AI.UniversalAI;
using AURA.Mobile.Diagnostics;

namespace AURA.Mobile.Controls;

/// <summary>
/// Config mínima: preset → key → modelo → Conectar.
/// DeepSeek (API oficial v4) e OpenRouter com free prioritários.
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
            UniversalApiFormat.OpenAiCompatible, true, "openrouter/free"),
        new("deepseek", "DeepSeek",
            "https://api.deepseek.com/chat/completions",
            "https://api.deepseek.com/models",
            UniversalApiFormat.OpenAiCompatible, true, "deepseek-v4-flash"),
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
        Placeholder = "Modelo (OpenRouter: openrouter/free · DeepSeek: deepseek-v4-flash)",
        FontSize = 13
    };
    private readonly Picker _modelPicker = new()
    {
        Title = "Modelos (free no topo)",
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
        Text = "Só free / listar",
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
        MaxLines = 5
    };
    private readonly List<string> _models = new();
    private readonly Dictionary<string, string> _displayToId = new(StringComparer.OrdinalIgnoreCase);
    private bool _advancedOpen;
    private bool _busy;
    private bool _freeOnly = true;

    public AiConfigView()
    {
        _presetPicker.ItemsSource = Presets.Select(p => p.Label).ToArray();
        _presetPicker.SelectedIndexChanged += OnPresetChanged;
        _modelPicker.ItemsSource = _models;
        _modelPicker.SelectedIndexChanged += (_, _) =>
        {
            if (_modelPicker.SelectedItem is not string display)
                return;
            if (_displayToId.TryGetValue(display, out var id))
                _modelEntry.Text = id;
            else
                _modelEntry.Text = display
                    .Replace(" (free)", "", StringComparison.OrdinalIgnoreCase)
                    .Replace(" (recomendado)", "", StringComparison.OrdinalIgnoreCase)
                    .Replace(" (legado)", "", StringComparison.OrdinalIgnoreCase)
                    .Trim();
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

        // Migrar modelo DeepSeek legado / free descontinuado no OpenRouter
        var model = RuntimeConfig.Model;
        var migrated = UniversalModelDiscovery.MigrateDeprecatedOpenRouterModel(model);
        if (migrated != null)
            model = migrated;
        if (Presets[idx].Id == "deepseek" &&
            (string.IsNullOrWhiteSpace(model) || model.Contains('/')))
            model = Presets[idx].ModelHint;
        else if (string.IsNullOrWhiteSpace(model))
            model = Presets[idx].ModelHint;

        _modelEntry.Text = model;
        _apiKeyEntry.IsVisible = Presets[idx].RequiresKey || Presets[idx].Id == "custom";

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

        // DeepSeek: sempre endpoints oficiais se não estiver em modo avançado editando
        if (p.Id == "deepseek" && !keepExistingUrls)
        {
            _baseUrlEntry.Text = p.BaseUrl;
            _modelsUrlEntry.Text = p.ModelsUrl;
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
        ApplyModelList(fb);
    }

    private void ApplyModelList(IReadOnlyList<UniversalModel> models)
    {
        _models.Clear();
        _displayToId.Clear();
        foreach (var m in models)
        {
            if (string.IsNullOrWhiteSpace(m.Id))
                continue;
            var display = string.IsNullOrWhiteSpace(m.DisplayName) ? m.Id : m.DisplayName;
            if (!_displayToId.ContainsKey(display))
            {
                _displayToId[display] = m.Id;
                _models.Add(display);
            }
        }
        _modelPicker.ItemsSource = null;
        _modelPicker.ItemsSource = _models.ToList();
        _modelPicker.IsVisible = _models.Count > 0;
    }

    private void ApplyModelList(IList<string> ids)
    {
        var mapped = ids.Select(id => new UniversalModel(id, id, "")).ToList();
        ApplyModelList(mapped);
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
            // Alterna free-only em OpenRouter a cada toque longo? aqui: sempre free no OpenRouter
            var preset = SelectedPreset();
            _freeOnly = preset?.Id == "openrouter" || _freeOnly;

            SetStatus(_freeOnly ? "Carregando modelos free…" : "Carregando modelos…", true);
            var count = await LoadModelsFromApiAsync(freeOnly: _freeOnly);
            if (count > 0)
                SetStatus($"{count} modelo(s)" + (_freeOnly ? " free" : "") + " · escolha na lista ou digite o ID.", true);
            else
                SetStatus("Lista vazia — sugestões offline. Toque de novo ou digite o modelo.", true);
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

    private async Task<int> LoadModelsFromApiAsync(bool freeOnly = true)
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

        // DeepSeek models URL oficial sem /v1 também funciona; se 404, tenta /v1/models
        IReadOnlyList<UniversalModel> list = Array.Empty<UniversalModel>();
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        var urlsToTry = new List<string> { modelsUrl };
        if (provider == "deepseek")
        {
            if (!modelsUrl.Contains("/v1/"))
                urlsToTry.Add("https://api.deepseek.com/v1/models");
            else
                urlsToTry.Add("https://api.deepseek.com/models");
        }

        Exception? last = null;
        foreach (var url in urlsToTry.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                var p = UniversalProviderRegistry.Custom(
                    provider, baseUrl, url, preset.Format,
                    RuntimeConfig.AuthHeader, RuntimeConfig.AuthScheme, RuntimeConfig.RequiresApiKey);
                list = await new UniversalModelDiscovery(http).LoadAsync(p, key);
                if (list.Count > 0)
                {
                    _modelsUrlEntry.Text = url;
                    break;
                }
            }
            catch (Exception ex)
            {
                last = ex;
            }
        }

        if (list.Count == 0)
            list = UniversalModelDiscovery.FallbackSuggestions(provider);

        // OpenRouter: free only por padrão
        if (provider == "openrouter" || freeOnly)
            list = UniversalModelDiscovery.PrioritizeFree(list, max: 250, freeOnly: freeOnly || provider == "openrouter");
        else
            list = UniversalModelDiscovery.PrioritizeFree(list, max: 250, freeOnly: false);

        if (list.Count == 0)
            list = UniversalModelDiscovery.FallbackSuggestions(provider);

        ApplyModelList(list);
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

            // DeepSeek: endpoints oficiais sempre (a menos que Avançado com URL custom)
            if (preset.Id == "deepseek")
            {
                if (!_advancedOpen || string.IsNullOrWhiteSpace(_baseUrlEntry.Text))
                    _baseUrlEntry.Text = preset.BaseUrl;
                if (!_advancedOpen || string.IsNullOrWhiteSpace(_modelsUrlEntry.Text))
                    _modelsUrlEntry.Text = preset.ModelsUrl;
            }

            if (preset.Id == "openrouter" && !_advancedOpen)
            {
                _baseUrlEntry.Text = preset.BaseUrl;
                _modelsUrlEntry.Text = preset.ModelsUrl;
            }

            var baseUrl = EndpointValidator.Normalize(
                string.IsNullOrWhiteSpace(_baseUrlEntry.Text) ? preset.BaseUrl : _baseUrlEntry.Text);
            var modelsUrl = EndpointValidator.Normalize(
                string.IsNullOrWhiteSpace(_modelsUrlEntry.Text) ? preset.ModelsUrl : _modelsUrlEntry.Text);
            var model = StripDisplaySuffix(_modelEntry.Text);
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

            // Migrar IDs free DeepSeek descontinuados no OpenRouter
            var migrated = UniversalModelDiscovery.MigrateDeprecatedOpenRouterModel(model);
            if (migrated != null)
            {
                model = migrated;
                _modelEntry.Text = model;
                SetStatus("Modelo free DeepSeek descontinuado → openrouter/free.", true);
            }

            if (string.IsNullOrWhiteSpace(model))
            {
                SetStatus("Informe o modelo.", false);
                return;
            }

            // DeepSeek API direta: não usar IDs OpenRouter
            if (preset.Id == "deepseek" && model.Contains('/', StringComparison.Ordinal))
            {
                SetStatus("DeepSeek direto: use deepseek-v4-flash ou deepseek-v4-pro (não IDs openrouter/…).", false);
                _modelEntry.Text = "deepseek-v4-flash";
                return;
            }

            // OpenRouter: IDs com org/modelo; free preferido
            if (preset.Id == "openrouter")
            {
                if (model.StartsWith("deepseek-", StringComparison.OrdinalIgnoreCase) && !model.Contains('/'))
                {
                    SetStatus("No OpenRouter use IDs tipo openrouter/free ou deepseek/deepseek-r1 (pago).", false);
                    _modelEntry.Text = "openrouter/free";
                    return;
                }
                // Se usuário digitou modelo sem :free e sem barra, orientar
                if (!model.Contains('/') && !model.Equals("openrouter/free", StringComparison.OrdinalIgnoreCase))
                {
                    SetStatus("OpenRouter precisa do ID completo (ex.: openrouter/free ou google/gemma-4-31b-it:free).", false);
                    return;
                }
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
                    SetStatus(live.Message + " · config salva; confira key/modelo.", false);
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

            try
            {
                await LoadModelsFromApiAsync(freeOnly: preset.Id == "openrouter");
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

    private static string StripDisplaySuffix(string? text)
    {
        var model = (text ?? string.Empty).Trim();
        model = model.Replace(" (free)", "", StringComparison.OrdinalIgnoreCase)
            .Replace(" (recomendado)", "", StringComparison.OrdinalIgnoreCase)
            .Replace(" (legado)", "", StringComparison.OrdinalIgnoreCase)
            .Trim();
        return model;
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
