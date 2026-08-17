using AURA.Agents;
using AURA.AI;
using AURA.Mobile.Diagnostics;
using AURA.Mobile.Speech;

namespace AURA.Mobile.Pages;

public partial class ChatPage : ContentPage
{
    private readonly OpenRouterClient _client;
    private readonly AURA.Memory.MemoryStore _memory;
    private readonly AuraOrchestrator _orchestrator;
    private readonly VoiceAssistantService? _voice;
    private readonly AURA.Mobile.ProcessRegistry _processes;

    public ChatPage(OpenRouterClient client, AURA.Memory.MemoryStore memory,
        AuraOrchestrator orchestrator, AURA.Mobile.ProcessRegistry processes,
        VoiceAssistantService? voice = null)
    {
        InitializeComponent();
        _client = client;
        _memory = memory;
        _orchestrator = orchestrator;
        _processes = processes;
        _voice = voice;
        BindingContext = _processes;
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

        RuntimeConfig.Apply(_client);
        string apiKey = (ApiKeyEntry?.Text?.Trim() ?? RuntimeConfig.ApiKey ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(apiKey))
            RuntimeConfig.ApiKey = apiKey;

        SendButton.IsEnabled = false;
        BusyIndicator.IsRunning = true;
        BusyIndicator.IsVisible = true;
        AnswerLabel.Text = "Pensando...";

        var process = _processes.Begin("Assistente", "Chat", "Processando solicitação");

        try
        {
            string answer;
            if (string.IsNullOrWhiteSpace(RuntimeConfig.ApiKey) &&
                string.IsNullOrWhiteSpace(_client.Options.ApiKey))
            {
                AnswerLabel.Text = "Orquestrando (memória+busca+execução)...";
                process.Message = "Orquestrando memória, busca e execução";
                answer = await _orchestrator.ExecuteAsync(question);
            }
            else
            {
                string? readyError = RuntimeConfig.EnsureReadyForRequest(_client);
                if (readyError != null)
                {
                    AnswerLabel.Text = readyError;
                    _processes.Fail(process.Id, "Configuração da IA não está pronta");
                    return;
                }
                var assistant = new AiAssistant(_client, _memory);
                answer = await assistant.AskAsync(question);
            }

            AnswerLabel.Text = answer;
            _voice?.SetLastUtterance(answer);
            _processes.Complete(process.Id);
        }
        catch (Exception ex)
        {
            AnswerLabel.Text = "Erro: " + ex.Message;
            AuraLog.Exception("ChatPage.OnSendClicked", ex);
            _processes.Fail(process.Id, ex.Message);
        }
        finally
        {
            SendButton.IsEnabled = true;
            BusyIndicator.IsRunning = false;
            BusyIndicator.IsVisible = false;
        }
    }

    private async void OnProcessTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not Border border || border.BindingContext is not AURA.Mobile.ProcessInfo process)
            return;

        if (Application.Current?.MainPage is AURA.Mobile.MainPage mainPage)
            await mainPage.NavigateToProcessAsync(process.Target);
    }

    private void OnProviderChanged(object? sender, EventArgs e)
    {
    }
}