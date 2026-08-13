using AURA.AI;
using AURA.Mobile.Diagnostics;
using AURA.Mobile.Speech;

namespace AURA.Mobile.Pages;

public partial class ChatPage : ContentPage
{
    private readonly OpenRouterClient _client;
    private readonly AURA.Memory.MemoryStore _memory;
    private readonly VoiceAssistantService? _voice;

    public ChatPage(OpenRouterClient client, AURA.Memory.MemoryStore memory,
        VoiceAssistantService? voice = null)
    {
        InitializeComponent();
        _client = client;
        _memory = memory;
        _voice = voice;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        RuntimeConfig.Apply(_client);
        // Sync UI pickers com o que está em RuntimeConfig (se os controles existirem).
        try
        {
            if (ProviderPicker != null && ModelPicker != null)
            {
                // Os handlers de picker podem ser ligados no futuro; por ora
                // a config efetiva vem de RuntimeConfig / Preferences.
            }
        }
        catch
        {
            // ignore
        }
    }

    private async void OnSendClicked(object sender, EventArgs e)
    {
        // Garante que o client está alinhado com Preferences/RuntimeConfig.
        RuntimeConfig.Apply(_client);

        // Se o usuário digitou chave no Entry da própria página, usa-a.
        string entryKey = ApiKeyEntry?.Text?.Trim() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(entryKey))
        {
            _client.Options.ApiKey = entryKey;
            Preferences.Default.Set("ai_api_key", entryKey);
            RuntimeConfig.ApiKey = entryKey;
        }

        string apiKey = _client.Options.ApiKey ?? string.Empty;
        string question = QuestionEditor.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(question))
        {
            AnswerLabel.Text = "Digite uma pergunta primeiro.";
            return;
        }

        QuestionEditor.Text = string.Empty;

        if (!string.IsNullOrWhiteSpace(apiKey) &&
            (apiKey.Length > 200 ||
             apiKey.IndexOfAny(new[] { ' ', '\t', '\r', '\n' }) >= 0))
        {
            AnswerLabel.Text = "Chave de API inválida (parece conter texto de log). " +
                "Toque em 'Restaurar padrão' na aba Correções e digite a chave manualmente.";
            return;
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
            _voice?.SetLastUtterance(answer);
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

    // Handlers referenciados pelo XAML (ProviderPicker).
    private void OnProviderChanged(object? sender, EventArgs e)
    {
        // Persistência completa via RuntimeConfig fica para evolução futura;
        // o envio já aplica RuntimeConfig.Apply no client.
    }
}
