using AURA.AI;
using AURA.AI.Providers;
using AURA.Mobile.Diagnostics;

namespace AURA.Mobile.Controls;

/// <summary>
/// Painel de configuração da IA (provedor + modelo + chave + base URL).
/// </summary>
public sealed class AiConfigView : ContentView
{
    private readonly IApiKeyProviderResolver _resolver = new ApiKeyProviderResolver();

    private readonly Picker _providerPicker = new() { Title = "Provedor" };
    private readonly Picker _modelPicker = new() { Title = "Modelo" };
    private readonly Entry _customModelEntry = new()
    {
        Placeholder = "Modelo custom (opcional)",
        FontSize = 12,
    };
    private readonly Entry _baseUrlEntry = new()
    {
        Placeholder = "http://127.0.0.1:11435/v1/chat/completions",
        FontSize = 12,
        Keyboard = Keyboard.Url,
    };
    private readonly Label _apiKeyLabel = new()
    {
        Text = "CHAVE DE API",
        FontSize = 10,
        TextColor = Color.FromArgb("#7a7a90"),
    };
    private readonly Entry _apiKeyEntry = new()
    {
        Placeholder = "Cole a chave do provedor",
        IsPassword = true,
    };
    private readonly Button _detectButton = new()
    {
        Text = "Detectar/Testar provedor",
        FontSize = 12,
    };
    private readonly Label _detectStatus = new()
    {
        FontSize = 11,
        TextColor = Color.FromArgb("#a0a0b8"),
        LineBreakMode = LineBreakMode.WordWrap,
    };

    private OpenRouterClient? _client;
    private bool _applying;

    public AiConfigView()
    {
        _providerPicker.SelectedIndexChanged += OnProviderChanged;
        _apiKeyEntry.TextChanged += OnKeyTextChanged;
        _customModelEntry.TextChanged += OnCustomModelChanged;
        _baseUrlEntry.TextChanged += OnBaseUrlChanged;
        _detectButton.Clicked += OnDetectClicked;

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

        Content = new VerticalStackLayout
        {
            Spacing = 10,
            Children =
            {
                new Grid
                {
                    ColumnDefinitions =
                    {
                        new ColumnDefinition(GridLength.Star),
                        new ColumnDefinition(GridLength.Star),
                    },
                    ColumnSpacing = 10,
                    Children = { providerCol, modelCol },
                },
                new VerticalStackLayout
                {
                    Spacing = 3,
                    Children =
                    {
                        new Label
                        {
                            Text = "MODELO CUSTOM",
                            FontSize = 10,
                            TextColor = Color.FromArgb("#7a7a90"),
                        },
                        _customModelEntry,
                    },
                },
                new VerticalStackLayout
                {
                    Spacing = 3,
                    Children =
                    {
                        new Label
                        {
                            Text = "BASE URL (Ollama / servidor local — deixe vazio = catálogo)",
                            FontSize = 10,
                            TextColor = Color.FromArgb("#7a7a90"),
                        },
                        _baseUrlEntry,
                    },
                },
                new VerticalStackLayout
                {
                    Spacing = 3,
                    Children = { _apiKeyLabel, _apiKeyEntry },
                },
                new HorizontalStackLayout
                {
                    Spacing = 10,
                    Children = { _detectButton },
                },
                _detectStatus,
            },
        };
    }

    public void Load(OpenRouterClient client)
    {
        _client = client;
        _applying = true;
        try
        {
            string savedProvider = RuntimeConfig.Provider;
            string savedModel = RuntimeConfig.Model;
            _apiKeyEntry.Text = RuntimeConfig.ApiKey;
            _baseUrlEntry.Text = RuntimeConfig.BaseUrlOverride;

            if (_providerPicker.ItemsSource == null)
            {
                _providerPicker.ItemsSource = ProviderCatalog.Providers;
                _providerPicker.ItemDisplayBinding = new Binding(nameof(ProviderInfo.Name));
            }

            int providerIndex = 0;
            for (int i = 0; i < ProviderCatalog.Providers.Count; i++)
            {
                var p = ProviderCatalog.Providers[i];
                if (string.Equals(p.Id, savedProvider, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(p.Name, savedProvider, StringComparison.OrdinalIgnoreCase))
                {
                    providerIndex = i;
                    break;
                }
            }

            _providerPicker.SelectedIndex = providerIndex;
            PopulateModels(savedModel);
            ApplyToClient();
            RefreshStatusLine();
        }
        finally
        {
            _applying = false;
        }
    }

    private void OnProviderChanged(object? sender, EventArgs e)
    {
        if (_applying) return;
        _customModelEntry.Text = string.Empty;

        if (_providerPicker.SelectedItem is ProviderInfo p &&
            string.Equals(p.Id, "ollama", StringComparison.OrdinalIgnoreCase) &&
            string.IsNullOrWhiteSpace(_baseUrlEntry.Text))
        {
            _applying = true;
            try
            {
                _baseUrlEntry.Text = string.IsNullOrWhiteSpace(p.BaseUrl)
                    ? "http://127.0.0.1:11435/v1/chat/completions"
                    : p.BaseUrl;
            }
            finally { _applying = false; }
        }

        PopulateModels(null);
        ApplyAndPersist();
    }

    private void OnModelChanged(object? sender, EventArgs e)
    {
        if (_applying) return;
        if (_modelPicker.SelectedItem is ProviderModel)
            _customModelEntry.Text = string.Empty;
        ApplyAndPersist();
    }

    private void OnCustomModelChanged(object? sender, TextChangedEventArgs e)
    {
        if (_applying) return;
        ApplyAndPersist();
    }

    private void OnBaseUrlChanged(object? sender, TextChangedEventArgs e)
    {
        if (_applying) return;
        ApplyAndPersist();
    }

    private void OnKeyTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_applying) return;
        ApplyAndPersist();
        TryAutoDetect();
    }

    private void TryAutoDetect()
    {
        string key = _apiKeyEntry.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(key) || _client == null)
            return;

        var detection = _resolver.Detect(new ProviderCredential(key));
        if (!detection.IsConclusive || detection.Provider == null)
        {
            if (detection.Candidates.Count > 0)
                _ = TryAutoProbeAsync(key);
            return;
        }

        ProviderInfo detected = (ProviderInfo)detection.Provider;
        if (_providerPicker.SelectedItem is ProviderInfo current &&
            string.Equals(current.Id, detected.Id, StringComparison.OrdinalIgnoreCase))
        {
            _detectStatus.Text = "Provedor reconhecido: " + detected.Name + ".";
            return;
        }

        _detectStatus.Text = "Provedor reconhecido pela chave: " + detected.Name + ".";
        SelectProviderById(detected.Id);
    }

    private async Task TryAutoProbeAsync(string key)
    {
        if (_client == null) return;

        _detectStatus.Text = "Detectando provedor…";
        try
        {
            var credential = new ProviderCredential(key, allowProbe: true);
            ProviderDetectionResult result = await _resolver.ResolveAsync(credential);

            if (result.Provider != null && result.IsConclusive)
            {
                _resolver.ApplyToClient(_client, result);
                if (result.Provider is ProviderInfo pi)
                    SelectProviderById(pi.Id);
                else
                    SelectProviderById(result.Provider.Name);
                _detectStatus.Text = result.Message;
            }
            else
            {
                _detectStatus.Text = result.Message;
            }
        }
        catch (Exception ex)
        {
            _detectStatus.Text = "Falha ao detectar.";
            AuraLog.Exception("AiConfigView.TryAutoProbe", ex);
        }
    }

    private async void OnDetectClicked(object sender, EventArgs e)
    {
        if (_client == null) return;

        ApplyAndPersist();

        if (_providerPicker.SelectedItem is ProviderInfo localProv && !localProv.NeedsKey)
        {
            _detectButton.IsEnabled = false;
            _detectStatus.Text = "Testando " + localProv.Name + "…";
            try
            {
                RuntimeConfig.Apply(_client);
                string probe = await _client.ChatAsync("Responda apenas: OK");
                string snippet = (probe ?? string.Empty).Trim();
                if (snippet.Length > 120)
                    snippet = snippet.Substring(0, 120) + "…";
                _detectStatus.Text = string.IsNullOrWhiteSpace(snippet)
                    ? "Sem resposta de " + localProv.Name + " em " + _client.Options.BaseUrl
                    : "OK · " + localProv.Name + " · " + _client.Options.BaseUrl + " · " + snippet;
            }
            catch (Exception ex)
            {
                _detectStatus.Text = "Falha Ollama/local: " + ex.Message +
                                     "\nURL: " + (_client.Options.BaseUrl ?? "?");
                AuraLog.Exception("AiConfigView.TestLocal", ex);
            }
            finally
            {
                _detectButton.IsEnabled = true;
            }
            return;
        }

        string key = _apiKeyEntry.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(key))
        {
            _detectStatus.Text = "Digite uma chave ou escolha Ollama (local).";
            return;
        }

        if (_providerPicker.SelectedItem is ProviderInfo sel)
        {
            string? fmt = RuntimeConfig.ValidateApiKeyFormat(key, sel);
            if (fmt != null)
            {
                _detectStatus.Text = fmt;
                return;
            }
        }

        string? preferred = (_providerPicker.SelectedItem as ProviderInfo)?.Name;
        _detectButton.IsEnabled = false;
        _detectStatus.Text = "Testando provedor…";

        try
        {
            var credential = new ProviderCredential(key, allowProbe: true)
            {
                PreferredProviderName = preferred
            };

            ProviderDetectionResult result = await _resolver.ResolveAsync(credential);

            if (result.Provider != null && result.IsConclusive)
            {
                _resolver.ApplyToClient(_client, result);
                if (result.Provider is ProviderInfo pi)
                    SelectProviderById(pi.Id);
                else
                    SelectProviderById(result.Provider.Name);
                _detectStatus.Text = result.Message;
            }
            else
            {
                _detectStatus.Text = result.Message;
            }
        }
        catch (Exception ex)
        {
            _detectStatus.Text = "Falha ao testar provedor.";
            AuraLog.Exception("AiConfigView.OnDetectClicked", ex);
        }
        finally
        {
            _detectButton.IsEnabled = true;
        }
    }

    private void SelectProviderById(string idOrName)
    {
        _applying = true;
        try
        {
            for (int i = 0; i < ProviderCatalog.Providers.Count; i++)
            {
                var p = ProviderCatalog.Providers[i];
                if (string.Equals(p.Id, idOrName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(p.Name, idOrName, StringComparison.OrdinalIgnoreCase))
                {
                    _providerPicker.SelectedIndex = i;
                    break;
                }
            }

            PopulateModels(null);
            ApplyAndPersist();
        }
        finally
        {
            _applying = false;
        }
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
        bool inList = false;
        if (!string.IsNullOrWhiteSpace(savedModel))
        {
            foreach (var m in provider.Models)
            {
                if (string.Equals(m.Id, savedModel, StringComparison.OrdinalIgnoreCase))
                {
                    model = m.Id;
                    inList = true;
                    break;
                }
            }

            if (!inList)
            {
                _customModelEntry.Text = savedModel;
                model = savedModel;
            }
        }

        int modelIndex = 0;
        var models = provider.Models;
        if (models.Count > 0)
        {
            if (inList && !string.IsNullOrWhiteSpace(model))
            {
                for (int i = 0; i < models.Count; i++)
                {
                    if (string.Equals(models[i].Id, model, StringComparison.OrdinalIgnoreCase))
                    {
                        modelIndex = i;
                        break;
                    }
                }
            }
            else if (string.IsNullOrWhiteSpace(model) || !inList)
            {
                int freeIdx = -1;
                for (int i = 0; i < models.Count; i++)
                {
                    if (models[i].IsFree)
                    {
                        freeIdx = i;
                        break;
                    }
                }

                if (freeIdx >= 0)
                    modelIndex = freeIdx;
                else if (!string.IsNullOrWhiteSpace(provider.DefaultModelId))
                {
                    for (int i = 0; i < models.Count; i++)
                    {
                        if (string.Equals(models[i].Id, provider.DefaultModelId, StringComparison.OrdinalIgnoreCase))
                        {
                            modelIndex = i;
                            break;
                        }
                    }
                }
            }
        }

        _modelPicker.SelectedIndex = models.Count > 0 ? modelIndex : -1;

        string hint = provider.NeedsKey
            ? (string.IsNullOrWhiteSpace(provider.KeyHint) ? "Chave de API" : provider.KeyHint)
            : "Deixe vazio (provedor local)";
        _apiKeyEntry.Placeholder = hint;
        _apiKeyLabel.Text = provider.NeedsKey ? "Chave de API" : "Chave de API (opcional)";

        if (string.IsNullOrWhiteSpace(RuntimeConfig.BaseUrlOverride) &&
            string.Equals(provider.Id, "ollama", StringComparison.OrdinalIgnoreCase))
        {
            _baseUrlEntry.Placeholder = provider.BaseUrl;
        }

        _modelPicker.SelectedIndexChanged += OnModelChanged;
    }

    private void ApplyAndPersist()
    {
        OpenRouterClient? client = _client;
        if (client == null || _providerPicker.SelectedItem is not ProviderInfo provider)
            return;

        RuntimeConfig.Provider = provider.Id;

        string custom = _customModelEntry.Text?.Trim() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(custom))
            RuntimeConfig.Model = custom;
        else if (_modelPicker.SelectedItem is ProviderModel pm)
            RuntimeConfig.Model = pm.Id;

        RuntimeConfig.BaseUrlOverride = _baseUrlEntry.Text?.Trim() ?? string.Empty;

        string apiKey = _apiKeyEntry.Text?.Trim() ?? string.Empty;
        RuntimeConfig.ApiKey = apiKey;

        string? fmt = RuntimeConfig.ValidateApiKeyFormat(apiKey, provider);
        if (fmt != null && !string.IsNullOrWhiteSpace(apiKey))
            _detectStatus.Text = fmt;
        else
            RefreshStatusLine();

        ApplyToClient();
    }

    private void RefreshStatusLine()
    {
        if (_client != null)
            RuntimeConfig.Apply(_client);

        string status = RuntimeConfig.LastStatusMessage;
        if (string.IsNullOrWhiteSpace(status) && _providerPicker.SelectedItem is ProviderInfo p)
            status = p.Name + " · " + RuntimeConfig.Model;

        _detectStatus.Text = status;
    }

    public void ApplyToClient()
    {
        if (_client == null) return;
        RuntimeConfig.Apply(_client);
    }
}
