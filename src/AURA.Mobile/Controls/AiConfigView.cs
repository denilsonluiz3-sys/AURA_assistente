using AURA.AI;
using AURA.Mobile.Diagnostics;

namespace AURA.Mobile.Controls;

/// <summary>
/// Painel de configuração da IA (provedor + modelo + chave) compartilhado
/// entre Chat e Agente. Toda alteração persiste imediatamente em
/// RuntimeConfig/Preferences e é aplicada no OpenRouterClient — sem depender
/// do botão "Enviar" de uma aba específica.
/// </summary>
public sealed class AiConfigView : ContentView
{
    private readonly Picker _providerPicker = new() { Title = "Provedor" };
    private readonly Picker _modelPicker = new() { Title = "Modelo" };
    private readonly Label _apiKeyLabel = new()
    {
        Text = "CHAVE DE API",
        FontSize = 10,
        TextColor = Color.FromArgb("#7a7a90"),
    };
    private readonly Entry _apiKeyEntry = new()
    {
        Placeholder = "sk-or-… (deixe vazio se não precisar)",
        IsPassword = true,
    };

    private OpenRouterClient? _client;
    private bool _applying;

    public AiConfigView()
    {
        _providerPicker.SelectedIndexChanged += OnProviderChanged;
        _apiKeyEntry.TextChanged += OnKeyTextChanged;

        var providerCol = new VerticalStackLayout
        {
            Spacing = 3,
            Children =
            {
                new Label { Text = "PROVEDOR", FontSize = 10, TextColor = Color.FromArgb("#7a7a90") },
                _providerPicker,
            },
        };

        var modelCol = new VerticalStackLayout
        {
            Spacing = 3,
            Children =
            {
                new Label { Text = "MODELO", FontSize = 10, TextColor = Color.FromArgb("#7a7a90") },
                _modelPicker,
            },
        };

        var apiKeyCol = new VerticalStackLayout
        {
            Spacing = 3,
            Children = { _apiKeyLabel, _apiKeyEntry },
        };

        Content = new VerticalStackLayout
        {
            Spacing = 10,
            Children =
            {
                new Grid
                {
                    ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) },
                    ColumnSpacing = 10,
                    Children = { providerCol, modelCol },
                },
                apiKeyCol,
            },
        };
    }

    /// <summary>Carrega a configuração salva e aplica no client.</summary>
    public void Load(OpenRouterClient client)
    {
        _client = client;
        _applying = true;
        try
        {
            string savedProvider = RuntimeConfig.Provider;
            string savedModel = RuntimeConfig.Model;
            _apiKeyEntry.Text = RuntimeConfig.ApiKey;

            if (_providerPicker.ItemsSource == null)
            {
                _providerPicker.ItemsSource = ProviderCatalog.Providers;
            }

            int providerIndex = 0;
            for (int i = 0; i < ProviderCatalog.Providers.Count; i++)
            {
                if (string.Equals(ProviderCatalog.Providers[i].Name, savedProvider, StringComparison.OrdinalIgnoreCase))
                {
                    providerIndex = i;
                    break;
                }
            }

            _providerPicker.SelectedIndex = providerIndex;
            PopulateModels(savedModel);
            ApplyToClient();
        }
        finally
        {
            _applying = false;
        }
    }

    private void OnProviderChanged(object? sender, EventArgs e)
    {
        if (_applying)
        {
            return;
        }

        PopulateModels(null);
        ApplyAndPersist();
    }

    private void OnModelChanged(object? sender, EventArgs e)
    {
        if (_applying)
        {
            return;
        }

        ApplyAndPersist();
    }

    private void OnKeyTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_applying)
        {
            return;
        }

        ApplyAndPersist();
    }

    private void PopulateModels(string? savedModel)
    {
        _modelPicker.SelectedIndexChanged -= OnModelChanged;

        if (_providerPicker.SelectedItem is not ProviderInfo provider)
        {
            _modelPicker.ItemsSource = null;
            return;
        }

        _modelPicker.ItemsSource = provider.Models;
        _modelPicker.ItemDisplayBinding = new Binding(nameof(ProviderModel.Label));

        string model = string.Empty;
        if (!string.IsNullOrWhiteSpace(savedModel))
        {
            foreach (var m in provider.Models)
            {
                if (string.Equals(m.Id, savedModel, StringComparison.OrdinalIgnoreCase))
                {
                    model = m.Id;
                    break;
                }
            }
        }

        if (string.IsNullOrWhiteSpace(model) && provider.Models.Count > 0)
        {
            model = provider.Models[0].Id;
        }

        int modelIndex = 0;
        for (int i = 0; i < provider.Models.Count; i++)
        {
            if (string.Equals(provider.Models[i].Id, model, StringComparison.OrdinalIgnoreCase))
            {
                modelIndex = i;
                break;
            }
        }

        _modelPicker.SelectedIndex = modelIndex;

        string hint = provider.NeedsKey
            ? (string.IsNullOrWhiteSpace(provider.KeyHint) ? "Chave de API" : $"Chave ({provider.KeyHint})")
            : "Deixe vazio (provedor local)";
        _apiKeyEntry.Placeholder = hint;
        _apiKeyLabel.Text = provider.NeedsKey ? "Chave de API" : "Chave de API (opcional)";
        _apiKeyEntry.IsVisible = provider.NeedsKey;

        _modelPicker.SelectedIndexChanged += OnModelChanged;
    }

    private void ApplyAndPersist()
    {
        OpenRouterClient? client = _client;
        if (client == null || _providerPicker.SelectedItem is not ProviderInfo provider)
        {
            return;
        }

        RuntimeConfig.Provider = provider.Name;
        Preferences.Default.Set("ai_provider", provider.Name);
        client.Options.BaseUrl = provider.BaseUrl;

        if (_modelPicker.SelectedItem is ProviderModel pm)
        {
            RuntimeConfig.Model = pm.Id;
            Preferences.Default.Set("ai_model", pm.Id);
            client.Options.Model = pm.Id;
        }

        string apiKey = _apiKeyEntry.Text?.Trim() ?? string.Empty;
        RuntimeConfig.ApiKey = apiKey;
        Preferences.Default.Set("ai_api_key", apiKey);
        client.Options.ApiKey = apiKey;

        client.Options.MaxTokens = RuntimeConfig.MaxTokens;
        client.Options.TimeoutSeconds = RuntimeConfig.TimeoutSeconds;
        ApplyToClient();
    }

    /// <summary>Aplica a configuração atual no client (sem persistir).</summary>
    public void ApplyToClient()
    {
        if (_client == null)
        {
            return;
        }

        if (_providerPicker.SelectedItem is ProviderInfo provider)
        {
            _client.Options.BaseUrl = provider.BaseUrl;
        }

        if (_modelPicker.SelectedItem is ProviderModel pm)
        {
            _client.Options.Model = pm.Id;
        }

        _client.Options.MaxTokens = RuntimeConfig.MaxTokens;
        _client.Options.TimeoutSeconds = RuntimeConfig.TimeoutSeconds;
        _client.Options.ApiKey = RuntimeConfig.ApiKey;
    }
}
