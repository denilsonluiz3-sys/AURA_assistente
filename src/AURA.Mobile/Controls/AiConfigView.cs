using AURA.AI;
using AURA.AI.Providers;
using AURA.Mobile.Diagnostics;

namespace AURA.Mobile.Controls;

/// <summary>
/// Configuração independente: chave, provedor e modelo são estados diferentes.
/// A chave pode detectar o provedor quando houver evidência, mas nunca fixa o modelo.
/// </summary>
public sealed class AiConfigView : ContentView
{
    private readonly IApiKeyProviderResolver _resolver = new ApiKeyProviderResolver();
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

            string providerId = RuntimeConfig.Provider;
            SelectProvider(providerId);
            _apiKeyEntry.Text = RuntimeConfig.GetApiKeyForProvider(providerId);
            _baseUrlEntry.Text = RuntimeConfig.BaseUrlOverride;
            PopulateModels(RuntimeConfig.Model);
            RefreshStatus();
        }
        finally { _loading = false; }
    }

    private void OnProviderChanged(object? sender, EventArgs e)
    {
        if (_loading || _providerPicker.SelectedItem is not ProviderInfo provider) return;
        _loading = true;
        try
        {
            RuntimeConfig.Provider = provider.Id;
            // Trocar provedor invalida somente a seleção de modelo anterior.
            RuntimeConfig.Model = string.Empty;
            RuntimeConfig.BaseUrlOverride = string.Empty;
            _customModelEntry.Text = string.Empty;
            _baseUrlEntry.Text = provider.Id.Equals("ollama", StringComparison.OrdinalIgnoreCase) ? provider.BaseUrl : string.Empty;
            _apiKeyEntry.Text = RuntimeConfig.GetApiKeyForProvider(provider.Id);
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
        RefreshStatus();

        // Detecção é opcional: ela só pode mudar o provedor, nunca escolher um modelo.
        string key = e.NewTextValue?.Trim() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(key)) _ = DetectProviderAsync(key);
    }

    private async Task DetectProviderAsync(string key)
    {
        try
        {
            var result = await _resolver.ResolveAsync(new ProviderCredential(key, allowProbe: false));
            if (!result.IsConclusive || result.Provider is not ProviderInfo provider) return;

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                if (_loading) return;
                _loading = true;
                try
                {
                    RuntimeConfig.Provider = provider.Id;
                    SelectProvider(provider.Id);
                    _apiKeyEntry.Text = key;
                    // Não preservar modelo de outro provedor e não aplicar DefaultModelId aqui.
                    RuntimeConfig.Model = string.Empty;
                    PopulateModels(null);
                    ApplyToClient();
                    RefreshStatus();
                }
                finally { _loading = false; }
            });
        }
        catch (Exception ex) { AuraLog.Exception("AiConfigView.DetectProvider", ex); }
    }

    private void PopulateModels(string? selectedModel)
    {
        _modelPicker.SelectedIndexChanged -= OnModelChanged;
        try
        {
            if (_providerPicker.SelectedItem is not ProviderInfo provider)
            {
                _modelPicker.ItemsSource = null;
                return;
            }

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

            _customModelEntry.Text = selectedModel;
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
            if (string.Equals(provider.Id, providerId, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(provider.Name, providerId, StringComparison.OrdinalIgnoreCase))
            {
                _providerPicker.SelectedIndex = i;
                return;
            }
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

    private void ApplyToClient()
    {
        if (_client != null) RuntimeConfig.Apply(_client);
    }

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
            ApplyToClient();
            ProviderInfo? provider = ProviderCatalog.Find(RuntimeConfig.Provider);
            if (provider == null) { _status.Text = "Escolha um provedor."; return; }
            if (string.IsNullOrWhiteSpace(RuntimeConfig.Model)) { _status.Text = "Escolha um modelo."; return; }
            if (provider.NeedsKey && string.IsNullOrWhiteSpace(RuntimeConfig.GetApiKeyForProvider(provider.Id))) { _status.Text = "Informe a chave de API."; return; }

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
}
