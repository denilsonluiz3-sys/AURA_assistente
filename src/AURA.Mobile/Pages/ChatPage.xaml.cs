using AURA.AI;
using AURA.Mobile.Diagnostics;

namespace AURA.Mobile.Pages;

public partial class ChatPage : ContentPage
{
    private readonly OpenRouterClient _client;
    private readonly AURA.Memory.MemoryStore _memory;

    public ChatPage(OpenRouterClient client, AURA.Memory.MemoryStore memory)
    {
        InitializeComponent();
        _client = client;
        _memory = memory;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        RuntimeConfig.Apply(_client);

        string savedProvider = RuntimeConfig.Provider;
        string savedModel = RuntimeConfig.Model;
        ApiKeyEntry.Text = RuntimeConfig.ApiKey;

        if (ProviderPicker.ItemsSource == null)
        {
            ProviderPicker.ItemsSource = ProviderCatalog.Providers;
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

        ProviderPicker.SelectedIndex = providerIndex;
        ApplyProvider(savedProvider, savedModel);
    }

    private void OnProviderChanged(object sender, EventArgs e)
    {
        ApplyProvider(null, null);
    }

    private void ApplyProvider(string? savedProvider, string? savedModel)
    {
        if (ProviderPicker.SelectedItem is not ProviderInfo provider)
        {
            return;
        }

        Preferences.Default.Set("ai_provider", provider.Name);

        if (ModelPicker.ItemsSource == null || ModelPicker.SelectedItem == null)
        {
            ModelPicker.ItemsSource = provider.Models;
            ModelPicker.ItemDisplayBinding = new Microsoft.Maui.Controls.Binding(nameof(ProviderModel.Label));
        }

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

        ModelPicker.SelectedIndex = modelIndex;

        _client.Options.BaseUrl = provider.BaseUrl;
        _client.Options.Model = model;
        _client.Options.MaxTokens = RuntimeConfig.MaxTokens;
        _client.Options.TimeoutSeconds = RuntimeConfig.TimeoutSeconds;
        _client.Options.ApiKey = RuntimeConfig.ApiKey;

        ApiKeyEntry.Placeholder = provider.NeedsKey
            ? (string.IsNullOrWhiteSpace(provider.KeyHint) ? "Chave de API" : $"Chave ({provider.KeyHint})")
            : "Deixe vazio (provedor local)";
        ApiKeyLabel.Text = provider.NeedsKey ? "Chave de API" : "Chave de API (opcional)";
        ApiKeyLabel.IsVisible = true;
        ApiKeyEntry.IsVisible = provider.NeedsKey;
    }

    private async void OnSendClicked(object sender, EventArgs e)
    {
        string apiKey = ApiKeyEntry.Text?.Trim() ?? string.Empty;
        string question = QuestionEditor.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(question))
        {
            AnswerLabel.Text = "Digite uma pergunta primeiro.";
            return;
        }

        if (!string.IsNullOrWhiteSpace(apiKey) &&
            (apiKey.Length > 200 ||
             apiKey.IndexOfAny(new[] { ' ', '\t', '\r', '\n' }) >= 0))
        {
            AnswerLabel.Text = "Chave de API inválida (parece conter texto de log). " +
                "Toque em 'Restaurar padrão' na aba Correções e digite a chave manualmente.";
            return;
        }

        Preferences.Default.Set("ai_api_key", apiKey);
        RuntimeConfig.ApiKey = apiKey;
        _client.Options.ApiKey = apiKey;

        if (ModelPicker.SelectedItem is ProviderModel pm)
        {
            RuntimeConfig.Model = pm.Id;
            _client.Options.Model = pm.Id;
        }

        if (ProviderPicker.SelectedItem is ProviderInfo pi)
        {
            RuntimeConfig.Provider = pi.Name;
            _client.Options.BaseUrl = pi.BaseUrl;
        }

        if (string.IsNullOrWhiteSpace(apiKey) && (_client.Options.BaseUrl.Contains("openrouter") ||
            _client.Options.BaseUrl.Contains("groq") || _client.Options.BaseUrl.Contains("cerebras") ||
            _client.Options.BaseUrl.Contains("generativelanguage")))
        {
            AnswerLabel.Text = "Configure a chave de API para este provedor.";
            return;
        }

        SendButton.IsEnabled = false;
        BusyIndicator.IsRunning = true;
        BusyIndicator.IsVisible = true;
        AnswerLabel.Text = "Pensando...";

        try
        {
            var assistant = new AiAssistant(_client, _memory);
            string answer = await assistant.AskAsync(question);
            AnswerLabel.Text = answer;
        }
        catch (Exception ex)
        {
            AnswerLabel.Text = "Erro: " + ex.Message;
            AuraLog.Exception("ChatPage.OnSendClicked", ex);
        }
        finally
        {
            SendButton.IsEnabled = true;
            BusyIndicator.IsRunning = false;
            BusyIndicator.IsVisible = false;
        }
    }
}
