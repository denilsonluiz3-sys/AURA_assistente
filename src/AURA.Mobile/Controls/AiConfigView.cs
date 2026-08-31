using AURA.AI;
using AURA.Mobile.Diagnostics;

namespace AURA.Mobile.Controls;

/// <summary>
/// Configuração independente de provedor, modelo e credencial.
/// A chave nunca seleciona modelo ou endpoint.
/// </summary>
public sealed class AiConfigView : ContentView
{
    private readonly Picker _providerPicker = new() { Title = "Escolha o provedor" };
    private readonly Picker _modelPicker = new() { Title = "Escolha o modelo" };
    private readonly Entry _apiKeyEntry = new() { Placeholder = "Chave de API", IsPassword = true };
    private readonly Entry _customModelEntry = new() { Placeholder = "Modelo custom (opcional)", FontSize = 12 };
    private readonly Entry _baseUrlEntry = new() { Placeholder = "BASE URL (opcional)", FontSize = 12, Keyboard = Keyboard.Url };
    private readonly Label _status = new() { FontSize = 11, TextColor = Color.FromArgb("#a0a0b8"), LineBreakMode = LineBreakMode.WordWrap };
    private readonly Button _testButton = new() { Text = "Testar", FontSize = 12 };
    private readonly Button _clearModelButton = new() { Text = "Limpar modelo", FontSize = 12 };
    private OpenRouterClient? _client;
    private bool _loading;

    public AiConfigView()
    {
        _providerPicker.SelectedIndexChanged += OnProviderChanged;
        _modelPicker.SelectedIndexChanged += OnModelChanged;
        _apiKeyEntry.TextChanged += OnApiKeyChanged;
        _customModelEntry.TextChanged += OnCustomModelChanged;
        _baseUrlEntry.TextChanged += OnBaseUrlChanged;
        _testButton.Clicked += OnTestClicked;
        _clearModelButton.Clicked += OnClearModelClicked;

        var grid = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) },
            ColumnSpacing = 8
        };
        grid.Add(Field("PROVEDOR", _providerPicker), 0, 0);
        grid.Add(Field("MODELO", _modelPicker), 1, 0);

        Content = new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                grid,
                Field("CHAVE DE API", _apiKeyEntry),
                Field("MODELO CUSTOM", _customModelEntry),
                Field("BASE URL", _baseUrlEntry),
                new HorizontalStackLayout { Spacing = 8, Children = { _testButton, _clearModelButton } },
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
            if (_providerPicker.ItemsSource == null)
            {
                _providerPicker.ItemsSource = ProviderCatalog.Providers;
                _providerPicker.ItemDisplayBinding = new Binding(nameof(ProviderInfo.Name));
            }

            SelectProvider(RuntimeConfig.Provider);
            string providerId = (_providerPicker.SelectedItem as ProviderInfo)?.Id ?? RuntimeConfig.Provider;

            // Sanitiza estado antigo antes de repopular a UI. Uma configuração
            // criada para outro provedor jamais reaparece como modelo atual.
            SanitizePersistedState(providerId);

            _apiKeyEntry.Text = RuntimeConfig.GetApiKeyForProvider(providerId);
            _baseUrlEntry.Text = RuntimeConfig.BaseUrlOverride;
            PopulateModels(RuntimeConfig.Model);
            RefreshStatus();
        }
        finally { _loading = false; }
    }

    private static void SanitizePersistedState(string providerId)
    {
        ProviderInfo? provider = ProviderCatalog.Find(providerId);
        if (provider == null)
        {
            RuntimeConfig.Model = string.Empty;
            RuntimeConfig.BaseUrlOverride = string.Empty;
            return;
        }

        string model = RuntimeConfig.Model;
        if (!string.IsNullOrWhiteSpace(model) && ProviderCatalog.FindModel(provider.Id, model) == null)
            RuntimeConfig.Model = string.Empty;

        // OpenRouter, Gemini, OpenAI, Groq etc. devem usar o endpoint do catálogo.
        // A URL local do Ollama não pode sobreviver à troca para um provedor remoto.
        string baseUrl = RuntimeConfig.BaseUrlOverride;
        if (provider.Id.Equals("openrouter", StringComparison.OrdinalIgnoreCase) &&
            (baseUrl.Contains("127.0.0.1", StringComparison.OrdinalIgnoreCase) || baseUrl.Contains("localhost", StringComparison.OrdinalIgnoreCase)))
            RuntimeConfig.BaseUrlOverride = string.Empty;
    }

    private void OnProviderChanged(object? sender, EventArgs e)
    {
        if (_loading || _providerPicker.SelectedItem is not ProviderInfo provider) return;
        _loading = true;
        try
        {
            RuntimeConfig.Provider = provider.Id;
            RuntimeConfig.Model = string.Empty;
            RuntimeConfig.BaseUrlOverride = string.Empty;
            _apiKeyEntry.Text = RuntimeConfig.GetApiKeyForProvider(provider.Id);
            _baseUrlEntry.Text = provider.Id.Equals("ollama", StringComparison.OrdinalIgnoreCase) ? provider.BaseUrl : string.Empty;
            PopulateModels(null);
            ApplyToClient();
            RefreshStatus();
        }
        finally { _loading = false; }
    }

    private void OnModelChanged(object? sender, EventArgs e)
    {
        if (_loading || _modelPicker.SelectedItem is not ProviderModel model) return;
        _customModelEntry.Text = string.Empty;
        RuntimeConfig.Model = model.Id;
        ApplyToClient();
        RefreshStatus();
    }

    private void OnApiKeyChanged(object? sender, TextChangedEventArgs e)
    {
        if (_loading) return;
        string providerId = (_providerPicker.SelectedItem as ProviderInfo)?.Id ?? RuntimeConfig.Provider;
        RuntimeConfig.SetApiKeyForProvider(providerId, e.NewTextValue);
        // Credencial é somente credencial. Não altera provedor, modelo ou URL.
        RefreshStatus();
    }

    private void PopulateModels(string? selectedModel)
    {
        _modelPicker.SelectedIndexChanged -= OnModelChanged;
        try
        {
            if (_providerPicker.SelectedItem is not ProviderInfo provider) { _modelPicker.ItemsSource = null; return; }
            _modelPicker.ItemsSource = provider.Models;
            _modelPicker.ItemDisplayBinding = new Binding(nameof(ProviderModel.Label));
            _modelPicker.SelectedIndex = -1;
            _customModelEntry.Text = string.Empty;
            if (string.IsNullOrWhiteSpace(selectedModel)) return;
            for (int i = 0; i < provider.Models.Count; i++)
            {
                if (string.Equals(provider.Models[i].Id, selectedModel, StringComparison.OrdinalIgnoreCase))
                {
                    _modelPicker.SelectedIndex = i;
                    return;
                }
            }
        }
        finally { _modelPicker.SelectedIndexChanged += OnModelChanged; }
    }

    private void SelectProvider(string? providerId)
    {
        _providerPicker.SelectedIndex = -1;
        if (string.IsNullOrWhiteSpace(providerId)) return;
        for (int i = 0; i < ProviderCatalog.Providers.Count; i++)
        {
            var provider = ProviderCatalog.Providers[i];
            if (string.Equals(provider.Id, providerId, StringComparison.OrdinalIgnoreCase) || string.Equals(provider.Name, providerId, StringComparison.OrdinalIgnoreCase)) { _providerPicker.SelectedIndex = i; return; }
        }
    }

    private void OnCustomModelChanged(object? sender, TextChangedEventArgs e)
    {
        if (_loading) return;
        string model = e.NewTextValue?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(model)) return;
        _modelPicker.SelectedIndex = -1;
        RuntimeConfig.Model = model;
        ApplyToClient();
        RefreshStatus();
    }

    private void OnBaseUrlChanged(object? sender, TextChangedEventArgs e)
    {
        if (_loading) return;
        RuntimeConfig.BaseUrlOverride = e.NewTextValue?.Trim() ?? string.Empty;
        ApplyToClient();
    }

    private void OnClearModelClicked(object? sender, EventArgs e)
    {
        if (_loading) return;
        _loading = true;
        try
        {
            RuntimeConfig.Model = string.Empty;
            _modelPicker.SelectedIndex = -1;
            _customModelEntry.Text = string.Empty;
            ApplyToClient();
            RefreshStatus();
        }
        finally { _loading = false; }
    }

    public void ApplyToClient() { if (_client != null) RuntimeConfig.Apply(_client); }

    private void RefreshStatus()
    {
        string provider = (_providerPicker.SelectedItem as ProviderInfo)?.Name ?? "nenhum provedor";
        string model = string.IsNullOrWhiteSpace(RuntimeConfig.Model) ? "nenhum modelo escolhido" : RuntimeConfig.Model;
        string key = string.IsNullOrWhiteSpace(_apiKeyEntry.Text) ? "sem chave" : "chave configurada";
        _status.Text = provider + " · " + key + " · " + model;
    }

    private async void OnTestClicked(object? sender, EventArgs e)
    {
        if (_client == null) return;
        try
        {
            ProviderInfo? provider = ProviderCatalog.Find(RuntimeConfig.Provider);
            if (provider == null) { _status.Text = "Escolha um provedor."; return; }
            if (string.IsNullOrWhiteSpace(RuntimeConfig.Model)) { _status.Text = "Escolha um modelo."; return; }
            if (provider.NeedsKey && string.IsNullOrWhiteSpace(RuntimeConfig.GetApiKeyForProvider(provider.Id))) { _status.Text = "Informe a chave de API."; return; }
            ApplyToClient();
            _testButton.IsEnabled = false;
            _status.Text = "Testando " + provider.Name + " · " + RuntimeConfig.Model + "…";
            string response = await _client.ChatAsync("Responda apenas: OK");
            string snippet = (response ?? string.Empty).Trim();
            if (snippet.Length > 120) snippet = snippet[..120] + "…";
            _status.Text = "OK · " + provider.Name + " · " + RuntimeConfig.Model + " · " + snippet;
        }
        catch (Exception ex) { _status.Text = "Falha: " + ex.Message; AuraLog.Exception("AiConfigView.Test", ex); }
        finally { _testButton.IsEnabled = true; }
    }
}
