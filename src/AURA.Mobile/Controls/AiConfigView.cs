using AURA.AI.UniversalAI;
using AURA.Mobile.Diagnostics;

namespace AURA.Mobile.Controls;

/// <summary>
/// Config mínima: preset → key → modelo → Conectar.
/// Avançado (endpoint/formato) fica recolhido.
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
        new("openrouter", "OpenRouter",
            "https://openrouter.ai/api/v1/chat/completions",
            "https://openrouter.ai/api/v1/models",
            UniversalApiFormat.OpenAiCompatible, true, "openai/gpt-4o-mini"),
        new("deepseek", "DeepSeek",
            "https://api.deepseek.com/chat/completions",
            "https://api.deepseek.com/models",
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
        Placeholder = "API key",
        IsPassword = true,
        ClearButtonVisibility = ClearButtonVisibility.WhileEditing,
        FontSize = 13
    };
    private readonly Entry _modelEntry = new()
    {
        Placeholder = "Modelo (ex.: deepseek-chat)",
        FontSize = 13
    };
    private readonly Picker _modelPicker = new()
    {
        Title = "Modelos carregados",
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
        MaxLines = 3
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
                _modelEntry.Text = m;
        };
        _advancedToggle.Clicked += OnAdvancedToggle;
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
                string.Equals(
                    EndpointValidator.Normalize(p.BaseUrl),
                    EndpointValidator.Normalize(RuntimeConfig.BaseUrlOverride),
                    StringComparison.OrdinalIgnoreCase));
        }

        if (idx < 0)
            idx = string.IsNullOrEmpty(provider) && string.IsNullOrEmpty(RuntimeConfig.BaseUrlOverride)
                ? 0
                : Presets.Length - 1; // custom

        _presetPicker.SelectedIndex = idx;
        ApplyPresetFields(Presets[idx], keepExistingUrls: true);

        _apiKeyEntry.Text = RuntimeConfig.GetApiKeyForProvider(
            string.IsNullOrEmpty(RuntimeConfig.Provider) ? Presets[idx].Id : RuntimeConfig.Provider);
        _modelEntry.Text = string.IsNullOrWhiteSpace(RuntimeConfig.Model)
            ? Presets[idx].ModelHint
            : RuntimeConfig.Model;

        _apiKeyEntry.IsVisible = Presets[idx].RequiresKey || Presets[idx].Id == "custom";
        RefreshStatusLine();
    }

    private void OnPresetChanged(object? sender, EventArgs e)
    {
        var p = SelectedPreset();
        if (p == null)
            return;

        ApplyPresetFields(p, keepExistingUrls: false);
        _apiKeyEntry.IsVisible = p.RequiresKey || p.Id == "custom";
        if (string.IsNullOrWhiteSpace(_modelEntry.Text) && !string.IsNullOrEmpty(p.ModelHint))
            _modelEntry.Text = p.ModelHint;

        if (p.Id == "custom")
            SetAdvanced(true);

        _status.Text = string.Empty;
    }

    private void ApplyPresetFields(Preset p, bool keepExistingUrls)
    {
        if (p.Id == "custom")
        {
            if (!keepExistingUrls)
            {
                // mantém o que o usuário já digitou
            }
            else
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

    private void OnAdvancedToggle(object? sender, EventArgs e)
        => SetAdvanced(!_advancedOpen);

    private void SetAdvanced(bool open)
    {
        _advancedOpen = open;
        _baseUrlEntry.IsVisible = open;
        _modelsUrlEntry.IsVisible = open;
        _advancedToggle.Text = open ? "▾ Avançado" : "▸ Avançado";
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
                // permitir nome curto no endpoint host
                var host = HostHint(_baseUrlEntry.Text);
                if (!string.IsNullOrEmpty(host))
                    provider = host;
            }

            var baseUrl = EndpointValidator.Normalize(
                string.IsNullOrWhiteSpace(_baseUrlEntry.Text) ? preset.BaseUrl : _baseUrlEntry.Text);
            var modelsUrl = EndpointValidator.Normalize(
                string.IsNullOrWhiteSpace(_modelsUrlEntry.Text) ? preset.ModelsUrl : _modelsUrlEntry.Text);
            var model = _modelEntry.Text?.Trim() ?? string.Empty;
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
                SetStatus("Informe o modelo.", false);
                return;
            }

            var requiresKey = preset.RequiresKey;
            if (preset.Id == "custom")
                requiresKey = !string.IsNullOrEmpty(key); // custom local pode sem key

            if (requiresKey || preset.Id != "ollama")
            {
                // ollama: key opcional
                if (preset.Id != "ollama")
                {
                    var keyCheck = ApiKeyValidator.ValidateFormat(key, provider, required: true);
                    if (!keyCheck.Success)
                    {
                        SetStatus(keyCheck.Message, false);
                        return;
                    }
                }
            }

            SetStatus("Conectando…", true);

            // Persistir
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

            // Probe leve da key (pula se ollama sem key)
            if (preset.Id != "ollama" && !string.IsNullOrEmpty(key))
            {
                var live = await ApiKeyValidator.VerifyLiveAsync(
                    key, baseUrl, model, preset.Format,
                    RuntimeConfig.AuthHeader, RuntimeConfig.AuthScheme,
                    provider, timeoutSeconds: 20);

                if (!live.Success)
                {
                    SetStatus(live.Message, false);
                    return;
                }

                SetStatus(Short(live.Message, 120), true);
            }
            else
            {
                var probe = await EndpointValidator.ProbeAsync(baseUrl, key, timeoutSeconds: 12);
                if (!probe.Success)
                {
                    SetStatus(probe.Message, false);
                    return;
                }
                SetStatus("Conectado · " + Short(probe.Message, 80), true);
            }

            // Tentar listar modelos em silêncio (não bloqueia sucesso)
            if (!string.IsNullOrWhiteSpace(modelsUrl))
            {
                try
                {
                    var p = UniversalProviderRegistry.Custom(
                        provider, baseUrl, modelsUrl, preset.Format,
                        RuntimeConfig.AuthHeader, RuntimeConfig.AuthScheme, RuntimeConfig.RequiresApiKey);
                    using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
                    var list = await new UniversalModelDiscovery(http).LoadAsync(p, key);
                    _models.Clear();
                    _models.AddRange(list.Select(x => x.Id).Take(40));
                    _modelPicker.ItemsSource = null;
                    _modelPicker.ItemsSource = _models;
                    _modelPicker.IsVisible = _models.Count > 0;
                    if (_models.Count > 0 && string.IsNullOrWhiteSpace(_modelEntry.Text))
                    {
                        _modelEntry.Text = _models[0];
                        RuntimeConfig.Model = _models[0];
                        if (client != null)
                            RuntimeConfig.Apply(client);
                    }
                }
                catch
                {
                    // lista opcional
                }
            }

            RefreshStatusLine();
            if (string.IsNullOrEmpty(_status.Text) || _status.TextColor == Colors.Green)
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
