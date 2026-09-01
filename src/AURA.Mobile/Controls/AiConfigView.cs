using System.Text.Json;
using AURA.AI;
using AURA.Mobile.Diagnostics;

namespace AURA.Mobile.Controls;

/// <summary>Configuração simples e universal: chave -> carregar modelos -> selecionar -> salvar.</summary>
public sealed class AiConfigView : ContentView
{
    private readonly Entry _apiKeyEntry = new()
    {
        Placeholder = "API key",
        IsPassword = true,
        ClearButtonVisibility = ClearButtonVisibility.WhileEditing
    };

    private readonly Button _loadModelsButton = new()
    {
        Text = "CARREGAR MODELOS",
        HorizontalOptions = LayoutOptions.Fill
    };

    private readonly Picker _modelPicker = new()
    {
        Title = "Selecione o modelo",
        IsEnabled = false,
        HorizontalOptions = LayoutOptions.Fill
    };

    private readonly Button _saveButton = new()
    {
        Text = "SALVAR",
        IsEnabled = false,
        HorizontalOptions = LayoutOptions.Fill
    };

    private readonly Label _status = new()
    {
        FontSize = 12,
        LineBreakMode = LineBreakMode.WordWrap
    };

    private readonly ActivityIndicator _busy = new()
    {
        IsVisible = false,
        IsRunning = false,
        HorizontalOptions = LayoutOptions.Center
    };

    private readonly List<string> _models = new();
    private OpenRouterClient? _client;
    private bool _loading;

    public AiConfigView()
    {
        _modelPicker.ItemsSource = _models;
        _loadModelsButton.Clicked += OnLoadModelsClicked;
        _saveButton.Clicked += OnSaveClicked;
        Loaded += OnLoaded;

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(20, 16),
                Spacing = 12,
                Children =
                {
                    new Label
                    {
                        Text = "Configuração de IA",
                        FontSize = 24,
                        FontAttributes = FontAttributes.Bold
                    },
                    new Label
                    {
                        Text = "API KEY",
                        FontAttributes = FontAttributes.Bold
                    },
                    _apiKeyEntry,
                    _loadModelsButton,
                    _busy,
                    new Label
                    {
                        Text = "MODELO",
                        FontAttributes = FontAttributes.Bold
                    },
                    _modelPicker,
                    _saveButton,
                    _status
                }
            }
        };
    }

    public void Load(OpenRouterClient client)
    {
        _client = client;
        LoadExisting();
    }

    private void OnLoaded(object? sender, EventArgs e) => LoadExisting();

    private void LoadExisting()
    {
        if (_loading) return;
        _loading = true;
        try
        {
            string providerId = RuntimeConfig.Provider;
            if (!string.IsNullOrWhiteSpace(providerId))
                _apiKeyEntry.Text = RuntimeConfig.GetApiKeyForProvider(providerId);

            string model = RuntimeConfig.Model;
            if (!string.IsNullOrWhiteSpace(model))
            {
                _models.Clear();
                _models.Add(model);
                _modelPicker.ItemsSource = null;
                _modelPicker.ItemsSource = _models;
                _modelPicker.SelectedItem = model;
                _modelPicker.IsEnabled = true;
                _saveButton.IsEnabled = !string.IsNullOrWhiteSpace(_apiKeyEntry.Text);
            }
        }
        catch { }
        finally
        {
            _loading = false;
        }
    }

    private async void OnLoadModelsClicked(object? sender, EventArgs e)
    {
        if (_loading) return;

        string key = _apiKeyEntry.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(key))
        {
            SetStatus("Informe a API key.", false);
            return;
        }

        SetBusy(true);
        _models.Clear();
        _modelPicker.ItemsSource = null;
        _modelPicker.IsEnabled = false;
        _saveButton.IsEnabled = false;

        try
        {
            var discovered = await DiscoverProviderAndModelsAsync(key);
            if (discovered == null)
            {
                SetStatus("Não foi possível identificar o provedor ou carregar os modelos com essa chave.", false);
                return;
            }

            RuntimeConfig.Provider = discovered.Provider.Id;
            RuntimeConfig.ApiFormat = discovered.Provider.ApiFormat;
            RuntimeConfig.BaseUrlOverride = string.Empty;
            RuntimeConfig.ModelsUrlOverride = discovered.Provider.ModelsUrl;
            RuntimeConfig.SetApiKeyForProvider(discovered.Provider.Id, key);

            _models.AddRange(discovered.Models);
            _modelPicker.ItemsSource = _models;
            _modelPicker.IsEnabled = _models.Count > 0;
            if (_models.Count > 0)
                _modelPicker.SelectedItem = _models[0];

            _saveButton.IsEnabled = _models.Count > 0;
            SetStatus($"{discovered.Provider.Name}: {_models.Count} modelo(s) carregado(s).", true);
        }
        catch (Exception ex)
        {
            SetStatus("Falha ao carregar modelos: " + ex.Message, false);
            AuraLog.Exception("AiConfigView.DiscoverModels", ex);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task<DiscoveryResult?> DiscoverProviderAndModelsAsync(string key)
    {
        var candidates = ProviderCatalog.Providers
            .Where(p => p.NeedsKey && !string.IsNullOrWhiteSpace(p.ModelsUrl))
            .ToList();

        if (candidates.Count == 0)
            return null;

        using var http = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(Math.Clamp(RuntimeConfig.TimeoutSeconds, 5, 30))
        };

        foreach (var provider in candidates)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, provider.ModelsUrl);
                string header = string.IsNullOrWhiteSpace(provider.AuthHeaderName)
                    ? "Authorization"
                    : provider.AuthHeaderName;
                string scheme = provider.AuthScheme ?? "Bearer ";
                request.Headers.TryAddWithoutValidation(header, scheme + key);

                using HttpResponseMessage response = await http.SendAsync(request).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                    continue;

                string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                List<string> models = ExtractModelIds(body);
                if (models.Count == 0)
                    continue;

                return new DiscoveryResult(provider, models);
            }
            catch
            {
                // Uma chave é testada contra os endpoints conhecidos até uma API responder.
            }
        }

        return null;
    }

    private static List<string> ExtractModelIds(string body)
    {
        var result = new List<string>();
        using JsonDocument document = JsonDocument.Parse(body);
        JsonElement root = document.RootElement;

        IEnumerable<JsonElement> items =
            root.ValueKind == JsonValueKind.Object &&
            root.TryGetProperty("data", out JsonElement data) &&
            data.ValueKind == JsonValueKind.Array
                ? data.EnumerateArray()
                : root.ValueKind == JsonValueKind.Object &&
                  root.TryGetProperty("models", out JsonElement models) &&
                  models.ValueKind == JsonValueKind.Array
                    ? models.EnumerateArray()
                    : root.ValueKind == JsonValueKind.Array
                        ? root.EnumerateArray()
                        : Enumerable.Empty<JsonElement>();

        foreach (JsonElement item in items)
        {
            if (item.ValueKind == JsonValueKind.String)
            {
                Add(item.GetString());
                continue;
            }

            if (item.ValueKind != JsonValueKind.Object)
                continue;

            foreach (string property in new[] { "id", "name", "model" })
            {
                if (item.TryGetProperty(property, out JsonElement value) &&
                    value.ValueKind == JsonValueKind.String)
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
            if (!string.IsNullOrWhiteSpace(id) &&
                !result.Contains(id, StringComparer.OrdinalIgnoreCase))
            {
                result.Add(id);
            }
        }
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        string key = _apiKeyEntry.Text?.Trim() ?? string.Empty;
        string model = _modelPicker.SelectedItem?.ToString() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(model))
        {
            SetStatus("Carregue os modelos e selecione um modelo.", false);
            return;
        }

        try
        {
            RuntimeConfig.SetApiKeyForProvider(RuntimeConfig.Provider, key);
            RuntimeConfig.Model = model;
            if (_client != null)
                RuntimeConfig.Apply(_client);

            SetStatus("Configuração salva.", true);
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            SetStatus("Falha ao salvar: " + ex.Message, false);
        }
    }

    private void SetBusy(bool busy)
    {
        _busy.IsVisible = busy;
        _busy.IsRunning = busy;
        _apiKeyEntry.IsEnabled = !busy;
        _loadModelsButton.IsEnabled = !busy;
    }

    private void SetStatus(string message, bool success)
    {
        _status.Text = message;
        _status.TextColor = success ? Colors.Green : Colors.Red;
    }

    private sealed record DiscoveryResult(ProviderInfo Provider, List<string> Models);
}
