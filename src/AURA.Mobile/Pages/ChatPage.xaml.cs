using AURA.AI;

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
        ApiKeyEntry.Text = Preferences.Default.Get("openrouter_key", string.Empty);
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

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            AnswerLabel.Text = "Configure a chave OpenRouter para usar o assistente.";
            return;
        }

        Preferences.Default.Set("openrouter_key", apiKey);
        _client.Options.ApiKey = apiKey;

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
