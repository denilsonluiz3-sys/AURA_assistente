using AURA.AI.UniversalAI;
using AURA.AI;
using AURA.Abstractions.Orchestration;
using AURA.Abstractions.Process;
using AURA.Agents;
using AURA.Mobile.Diagnostics;
using AURA.Mobile.Speech;

namespace AURA.Mobile.Pages;

public partial class ChatPage : ContentPage
{
    private readonly IUniversalAiClient _client;
    private readonly AURA.Memory.MemoryStore _memory;
    private readonly IProcessOrchestrator _processEngine;
    private readonly IOrchestrator _orchestrator;
    private readonly IIntentResolver _intentResolver;
    private readonly VoiceAssistantService? _voice;
    private readonly AURA.Mobile.ProcessRegistry _processes;

    public ChatPage(
        IUniversalAiClient client,
        AURA.Memory.MemoryStore memory,
        IProcessOrchestrator processEngine,
        IOrchestrator orchestrator,
        IIntentResolver intentResolver,
        AURA.Mobile.ProcessRegistry processes,
        VoiceAssistantService? voice = null)
    {
        InitializeComponent();
        _client = client;
        _memory = memory;
        _processEngine = processEngine;
        _orchestrator = orchestrator;
        _intentResolver = intentResolver;
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
            RuntimeConfig.ApiKey = entryKey;

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
            var intent = _intentResolver.Resolve(question);

            if (intent.Intent == "navigate"
                && intent.Parameters.TryGetValue("page", out string? pageLabel)
                && !string.IsNullOrWhiteSpace(pageLabel))
            {
                AnswerLabel.Text = $"Abrindo {pageLabel}…";
                _processes.Complete(process.Id);
                if (Application.Current?.Windows?.FirstOrDefault()?.Page is MainPage main)
                    await main.NavigateToProcessAsync(pageLabel);
                else if (Application.Current?.MainPage is MainPage mainLegacy)
                    await mainLegacy.NavigateToProcessAsync(pageLabel);
                return;
            }

            if (intent.Intent == "android"
                || intent.Confidence >= 0.85)
            {
                string orchResult = await _orchestrator.ExecuteAsync(question);
                AnswerLabel.Text = orchResult;
                _voice?.SetLastUtterance(orchResult);
                _processes.Complete(process.Id);
                return;
            }

            string answer;
            LlmHandler? llm = null;
            if (!string.IsNullOrWhiteSpace(RuntimeConfig.ApiKey) ||
                !string.IsNullOrWhiteSpace(_client.Options.ApiKey))
            {
                string? readyError = RuntimeConfig.EnsureReadyForRequest(_client);
                if (readyError != null)
                {
                    AnswerLabel.Text = readyError;
                    _processes.Fail(process.Id, "Configuração da IA não está pronta");
                    return;
                }
                var assistant = new AiAssistant(_client, _memory);
                llm = (prompt, ct) => assistant.AskAsync(prompt);
            }

            answer = await _processEngine.RunAsync(question, llm);

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

        if (Application.Current?.Windows?.FirstOrDefault()?.Page is MainPage main)
            await main.NavigateToProcessAsync(process.Target);
        else if (Application.Current?.MainPage is MainPage mainLegacy)
            await mainLegacy.NavigateToProcessAsync(process.Target);
    }

    private void OnProviderChanged(object? sender, EventArgs e)
    {
    }
}
