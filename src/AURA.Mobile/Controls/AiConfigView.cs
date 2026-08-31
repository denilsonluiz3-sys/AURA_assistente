using System.Text.Json;
using AURA.AI;
using AURA.AI.Providers;
using AURA.Mobile.Diagnostics;

namespace AURA.Mobile.Controls;

/// <summary>
/// Configuração universal de IA: provedor, formato, endpoint, credencial e modelo.
/// A lista de modelos pode ser descoberta diretamente no endpoint do provedor.
/// </summary>
public sealed class AiConfigView : ContentView
{
    private readonly Picker _providerPicker = new() { Title = "Provedor" };
    private readonly Picker _formatPicker = new() { Title = "Formato da API" };
    private readonly Picker _modelPicker = new() { Title = "Modelo" };
    private readonly Entry _apiKeyEntry = new() { Placeholder = "API key / token", IsPassword = true };
    private readonly Entry _baseUrlEntry = new() { Placeholder = "https://.../chat/completions", Keyboard = Keyboard.Url };
    private readonly Entry _modelsUrlEntry = new() { Placeholder = "https://.../models", Keyboard = Keyboard.Url };
    private readonly Entry _customModelEntry = new() { Placeholder = "ID do modelo (se não aparecer)" };
    private readonly Button _loadModelsButton = new() { Text = "Carregar modelos", FontSize = 12 };
    private readonly Button _testButton = new() { Text = "Testar", FontSize = 12 };
    private readonly Button _clearButton = new() { Text = "Limpar", FontSize = 12 };
    private readonly Label _status = new() { FontSize = 11, TextColor = Color.FromArgb("#a0a0b8"), LineBreakMode = LineBreakMode.WordWrap };
    private OpenRouterClient? _client;
    private bool _loading;

    private static readonly string[] ApiFormats = { "OpenAI-compatible", "Anthropic Messages" };

    public AiConfigView()
    {
        _providerPicker.SelectedIndexChanged += OnProviderChanged;
        _formatPicker.SelectedIndexChanged += OnFormatChanged;
        _modelPicker.SelectedIndexChanged += OnModelChanged;
        _apiKeyEntry.TextChanged += OnApiKeyChanged;
        _baseUrlEntry.TextChanged += OnBaseUrlChanged;
        _modelsUrlEntry.TextChanged += OnModelsUrlChanged;
        _customModelEntry.TextChanged += OnCustomModelChanged;
        _loadModelsButton.Clicked += OnLoadModelsClicked;
        _testButton.Clicked += OnTestClicked;
        _clearButton.Clicked += OnClearClicked;

        _formatPicker.ItemsSource = ApiFormats;

        var grid = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) },
            ColumnSpacing = 8
        };
        grid.Add(Field("PROVEDOR", _providerPicker), 0, 0);
        grid.Add(Field("FORMATO", _formatPicker), 1, 0);

        Content = new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                grid,
                Field("MODELO", _modelPicker),
                _customModelEntry,
                Field("API KEY / TOKEN", _apiKeyEntry),
                Field("CHAT ENDPOINT", _baseUrlEntry),
                Field("MODELS ENDPOINT", _modelsUrlEntry),
                new HorizontalStackLayout { Spacing = 8, Children = { _loadModelsButton, _testButton, _clearButton } },
                _status
            }
        };
    }

    private static VerticalStackLayout Field(string title, View view) => new()
    {
        Spacing = 2,
        Children = { new Label { Text = title, FontSize = 10, TextColor = Color.FromArgb("#7a7a90") }, view }
    };

    public void Load(OpenRouterClient client)
    {
        _client = client;
        _loading = true;
        try
        {
            _providerPicker.ItemsSource = ProviderCatalog.Providers;
            _providerPicker.ItemDisplayBinding = new Binding(nameof(ProviderInfo.Name));
            SelectProvider(RuntimeConfig.Provider);

            var provider = _providerPicker.SelectedItem as ProviderInfo ?? ProviderCatalog.Providers.FirstOrDefault();
            if (provider != null)
                PopulateProvider(provider, RuntimeConfig.Model);
            RefreshStatus();
        }
        finally { _loading = false; }
    }

    private void SelectProvider(string? providerId)
    {
        _providerPicker.SelectedIndex = -1;
        if (string.IsNullOrWhiteSpace(providerId))
            return;
        for (int i = 0; i < ProviderCatalog.Providers.Count; i++)
        {
            var p = ProviderCatalog.Providers[i];
            if (string.Equals(p.Id, providerId, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(p.Name, providerId, StringComparison.OrdinalIgnoreCase))
            {
                _providerPicker.SelectedIndex = i;
                return;
            }
        }
    }

    private void PopulateProvider(ProviderInfo provider, string? selectedModel)
    {
        _apiKeyEntry.Text = RuntimeConfig.GetApiKeyForProvider(provider.Id);
        _baseUrlEntry.Text = string.IsNullOrWhiteSpace(RuntimeConfig.BaseUrlOverride) ? provider.BaseUrl : RuntimeConfig.BaseUrlOverride;
        _modelsUrlEntry.Text = provider.ModelsUrl;

        _formatPicker.SelectedIndex = RuntimeConfig.ApiFormat == AiApiFormat.AnthropicMessages ? 1 : 0;
        PopulateModels(provider, selectedModel);
    }

    private void PopulateModels(ProviderInfo provider, string? selectedModel)
    {
        _modelPicker.SelectedIndexChanged -= OnModelChanged;
        try
        {
            _modelPicker.ItemsSource = provider.Models;
            _modelPicker.ItemDisplayBinding = new Binding(nameof(ProviderModel.Label));
            _modelPicker.SelectedIndex = -1;
            if (string.IsNullOrWhiteSpace(selectedModel))
                return;

            for (int i = 0; i < provider.Models.Count; i++)
            {
                if (string.Equals(provider.Models[i].Id, selectedModel, StringComparison.OrdinalIgnoreCase))
                {
                    _modelPicker.SelectedIndex = i;
                    return;
                }
            }

            _customModelEntry.Text = selectedModel;
        }
        finally { _modelPicker.SelectedIndexChanged += OnModelChanged; }
    }

    private void OnProviderChanged(object? sender, EventArgs e)
    {
        if (_loading || _providerPicker.SelectedItem is not ProviderInfo provider)
            return;

        _loading = true;
        try
        {
            RuntimeConfig.Provider = provider.Id;
            RuntimeConfig.Model = string.Empty;
            RuntimeConfig.BaseUrlOverride = string.Empty;
            RuntimeConfig.ApiFormat = provider.ApiFormat;
            _customModelEntry.Text = string.Empty;
            PopulateProvider(provider, null);
            ApplyToClient();
            RefreshStatus();
        }
        finally { _loading = false; }
    }

    private void OnFormatChanged(object? sender, EventArgs e)
    {
        if (_loading)
            return;
        RuntimeConfig.ApiFormat = _formatPicker.SelectedIndex == 1
            ? AiApiFormat.AnthropicMessages
            : AiApiFormat.OpenAICompletions;
        ApplyToClient();
        RefreshStatus();
    }

    private void OnModelChanged(object? sender, EventArgs e)
    {
        if (_loading || _modelPicker.SelectedItem is not ProviderModel model)
            return;
        _customModelEntry.TextChanged -= OnCustomModelChanged;
        try { _customModelEntry.Text = string.Empty; }
        finally { _customModelEntry.TextChanged += OnCustomModelChanged; }
        RuntimeConfig.Model = model.Id;
        ApplyToClient();
        RefreshStatus();
    }

    private void OnCustomModelChanged(object? sender, TextChangedEventArgs e)
    {
        if (_loading)
            return;
        string model = e.NewTextValue?.Trim() ?? string.Empty;
        RuntimeConfig.Model = model;
        _modelPicker.SelectedIndex = -1;
        ApplyToClient();
        RefreshStatus();
    }

    private void OnApiKeyChanged(object? sender, TextChangedEventArgs e)
    {
        if (_loading)
            return;
        string providerId = (_providerPicker.SelectedItem as ProviderInfo)?.Id ?? RuntimeConfig.Provider;
        RuntimeConfig.SetApiKeyForProvider(providerId, e.NewTextValue);
        ApplyToClient();
        RefreshStatus();
    }

    private void OnBaseUrlChanged(object? sender, TextChangedEventArgs e)
    {
        if (_loading)
            return;
        RuntimeConfig.BaseUrlOverride = e.NewTextValue?.Trim() ?? string.Empty;
        ApplyToClient();
    }

    private void OnModelsUrlChanged(object? sender, TextChangedEventArgs e)
    {
        if (_loading)
            return;
        if (_providerPicker.SelectedItem is ProviderInfo provider)
            provider.ModelsUrl = e.NewTextValue?.Trim() ?? string.Empty;
    }

    private async void OnLoadModelsClicked(object? sender, EventArgs e)
    {
        if (_providerPicker.SelectedItem is not ProviderInfo provider)
        {
            _status.Text = "Escolha um provedor.";
            return;
        }

        string url = _modelsUrlEntry.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(url))
        {
            _status.Text = "Informe o endpoint de modelos.";
            return;
        }

        _loadModelsButton.IsEnabled = false;
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(Math.Clamp(RuntimeConfig.TimeoutSeconds, 5, 300)) };
            string key = RuntimeConfig.GetApiKeyForProvider(provider.Id);
            if (!string.IsNullOrWhiteSpace(key))
            {
                string header = string.IsNullOrWhiteSpace(provider.AuthHeaderName) ? "Authorization" : provider.AuthHeaderName;
                string scheme = provider.AuthScheme ?? string.Empty;
                http.DefaultRequestHeaders.TryAddWithoutValidation(header, scheme + key);
            }

            string body = await http.GetStringAsync(url);
            var ids = ExtractModelIds(body);
            if (ids.Count == 0)
            {
                _status.Text = "Nenhum modelo reconhecido na resposta.";
                return;
            }

            provider.Models = ids.Select(id => new ProviderModel
            {
                Id = id,
                Label = id,
                Category = provider.Name,
                IsFree = false
            }).ToList();

            RuntimeConfig.Model = ids.Contains(RuntimeConfig.Model, StringComparer.OrdinalIgnoreCase)
                ? RuntimeConfig.Model
                : ids[0];
            PopulateModels(provider, RuntimeConfig.Model);
            ApplyToClient();
            RefreshStatus($"{ids.Count} modelos carregados");
        }
        catch (Exception ex)
        {
            _status.Text = "Falha ao carregar modelos: " + ex.Message;
            AuraLog.Exception("AiConfigView.LoadModels", ex);
        }
        finally { _loadModelsButton.IsEnabled = true; }
    }

    private static List<string> ExtractModelIds(string body)
    {
        var result = new List<string>();
        using var doc = JsonDocument.Parse(body);
        JsonElement root = doc.RootElement;

        IEnumerable<JsonElement> candidates = root.ValueKind == JsonValueKind.Object && root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array
            ? data.EnumerateArray()
            : root.ValueKind == JsonValueKind.Object && root.TryGetProperty("models", out var models) && models.ValueKind == JsonValueKind.Array
                ? models.EnumerateArray()
                : root.ValueKind == JsonValueKind.Array
                    ? root.EnumerateArray()
                    : Enumerable.Empty<JsonElement>();

        foreach (var item in candidates)
        {
            if (item.ValueKind == JsonValueKind.String)
            {
                Add(item.GetString());
                continue;
            }
            if (item.ValueKind != JsonValueKind.Object)
                continue;
            foreach (string name in new[] { "id", "name", "model" })
            {
                if (item.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String)
                {
                    Add(value.GetString());
                    break;
                }
            }
        }
        return result;

        void Add(string? value)
        {
            string id = value?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(id) && !result.Contains(id, StringComparer.OrdinalIgnoreCase))
                result.Add(id);
        }
    }

    public void ApplyToClient()
    {
        if (_client != null)
            RuntimeConfig.Apply(_client);
    }

    private void RefreshStatus(string? prefix = null)
    {
        string provider = (_providerPicker.SelectedItem as ProviderInfo)?.Name ?? "nenhum provedor";
        string model = string.IsNullOrWhiteSpace(RuntimeConfig.Model) ? "nenhum modelo" : RuntimeConfig.Model;
        string key = string.IsNullOrWhiteSpace(_apiKeyEntry.Text) ? "sem chave" : "chave configurada";
        _status.Text = string.IsNullOrWhiteSpace(prefix)
            ? provider + " · " + key + " · " + model
            : prefix + " · " + provider + " · " + model;
    }

    private async void OnTestClicked(object? sender, EventArgs e)
    {
        if (_client == null)
            return;
        try
        {
            var provider = ProviderCatalog.Find(RuntimeConfig.Provider);
            if (provider == null) { _status.Text = "Escolha um provedor."; return; }
            if (string.IsNullOrWhiteSpace(RuntimeConfig.Model)) { _status.Text = "Escolha ou informe um modelo."; return; }
            if (provider.NeedsKey && string.IsNullOrWhiteSpace(RuntimeConfig.GetApiKeyForProvider(provider.Id))) { _status.Text = "Informe a chave/token."; return; }

            ApplyToClient();
            _testButton.IsEnabled = false;
            _status.Text = "Testando " + provider.Name + " · " + RuntimeConfig.Model + "…";
            string response = await _client.ChatAsync("Responda apenas: OK");
            string snippet = (response ?? string.Empty).Trim();
            if (snippet.Length > 120) snippet = snippet[..120] + "…";
            _status.Text = "OK · " + provider.Name + " · " + RuntimeConfig.Model + " · " + snippet;
        }
        catch (Exception ex)
        {
            _status.Text = "Falha: " + ex.Message;
            AuraLog.Exception("AiConfigView.Test", ex);
        }
        finally { _testButton.IsEnabled = true; }
    }

    private void OnClearClicked(object? sender, EventArgs e)
    {
        if (_loading)
            return;
        RuntimeConfig.Model = string.Empty;
        RuntimeConfig.BaseUrlOverride = string.Empty;
        _modelPicker.SelectedIndex = -1;
        _customModelEntry.Text = string.Empty;
        ApplyToClient();
        RefreshStatus();
    }
}
