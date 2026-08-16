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
        try
        {
            if (ProviderPicker != null && ModelPicker != null)
            {
            }
        }
        catch
        {
        }
    }

    private async void OnSendClicked(object sender, EventArgs e)
    {
        string entryKey = ApiKeyEntry?.Text?.Trim() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(entryKey))
        {
            Preferences.Default.Set("ai_api_key", entryKey);
            RuntimeConfig.ApiKey = entryKey;
        }

        string question = QuestionEditor.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(question))
        {
            AnswerLabel.Text = "Digite uma pergunta primeiro.";
            return;
        }

        QuestionEditor.Text = string.Empty;

        // Mesma regra do Agent: Apply + fallback sem chave (Ollama / NeedsKey=false).
        string? readyError = RuntimeConfig.EnsureReadyForRequest(_client);
        if (readyError != null)
        {
            AnswerLabel.Text = readyError;
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

    private void OnProviderChanged(object? sender, EventArgs e)
    {
    }
}
