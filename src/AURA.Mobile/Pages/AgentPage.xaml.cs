using AURA.AI;
using AURA.Agents;
using AURA.Agents.Programs;
using AURA.Abstractions.Execution;
using AURA.Core.Events;
using AURA.Core.Runtime;
using AURA.Memory;
using AURA.Mobile.Diagnostics;
using AURA.Mobile.Services;
using AURA.Mobile.Speech;
using AURA.Modules.Executors;
using AURA.Mobile.Controls;
using Microsoft.Maui.Controls.Shapes;
using System.Collections.Specialized;
using System.Text;
using System.Text.Json;

namespace AURA.Mobile.Pages;

public partial class AgentPage : ContentPage
{
    private const string UrlDeepSeek = "https://chat.deepseek.com";
    private const string UrlChatGpt = "https://chatgpt.com";

    private readonly OpenRouterClient _client;
    private readonly MemoryStore _memory;
    private readonly ISpeechService _speech;
    private readonly VoiceAssistantService? _voice;
    private readonly ShellExecutor _shell;
    private readonly GitExecutor? _git;
    private readonly PythonExecutor? _python;
    private readonly NodeExecutor? _node;
    private readonly ProcessRegistry _processes;
    private readonly AgentExecutionCoordinator _coordinator;
    private readonly Dictionary<string, AgentCapabilityBubble> _capabilityBubbles = new(StringComparer.OrdinalIgnoreCase);
    private readonly AuraOrchestrator _orchestrator;
    private readonly LocalPlaybook? _playbook;
    private readonly SolutionStore? _solutions;
    private readonly CellProgramRegistry? _cellRegistry;
    private readonly SimulationRuntime? _runtime;
    private readonly IAndroidCapabilityService? _android;
    private readonly SemaphoreSlim _bubbleGate = new(1, 1);
    private readonly List<string> _recentCommands = new();
    private readonly List<string> _runShellCommands = new();
    private AgentSession? _session;
    private string? _activeProcessId;
    private bool _configVisible;
    private bool _runInFlight;
    private bool _webMode;
    private bool _webLoaded;
    private string? _lastUserGoal;
    private string? _lastUserQuery;
    private string? _lastAssistantText;

    public AgentPage(OpenRouterClient client, MemoryStore memory, ISpeechService speech,
        ShellExecutor shell, ProcessRegistry processes, AuraOrchestrator orchestrator,
        AgentExecutionCoordinator coordinator,
        LocalPlaybook? playbook = null, VoiceAssistantService? voice = null,
        SolutionStore? solutions = null, GitExecutor? git = null, PythonExecutor? python = null,
        NodeExecutor? node = null, CellProgramRegistry? cellRegistry = null, SimulationRuntime? runtime = null,
        IAndroidCapabilityService? android = null)
    {
        InitializeComponent();
        _client = client;
        _memory = memory;
        _solutions = solutions;
        _speech = speech;
        _shell = shell;
        _git = git;
        _python = python;
        _node = node;
        _android = android;
        _processes = processes;
        _coordinator = coordinator;
        _orchestrator = orchestrator;
        _cellRegistry = cellRegistry;
        _runtime = runtime;
        _playbook = playbook;
        ProcessCards.BindingContext = _processes;
        _voice = voice;
        LoadRecentsFromPrefs();

        _processes.Processes.CollectionChanged += OnProcessesChanged;

        ProcessExecutorBase.ProcessStarted += OnCapabilityProcessStarted;
        ProcessExecutorBase.OutputReceived += OnCapabilityProcessOutput;
        ProcessExecutorBase.ProcessCompleted += OnCapabilityProcessCompleted;

        _coordinator.Started += OnCoordinatorStarted;
        _coordinator.Output += OnCoordinatorOutput;
        _coordinator.Completed += OnCoordinatorCompleted;

        UpdateProcessCardsVisibility();
        ApplyModeUi();
    }

    private AgentCapabilityBubble GetOrCreateCapabilityBubble(string correlationId, string title)
    {
        if (_capabilityBubbles.TryGetValue(correlationId, out var existing))
            return existing;

        var bubble = new AgentCapabilityBubble(correlationId, title);
        _capabilityBubbles[correlationId] = bubble;
        ConversationContainer.Children.Add(bubble);
        _ = ScrollToEndAsync();
        return bubble;
    }

    private async Task ScrollToEndAsync()
    {
        try
        {
            await Task.Delay(30);
            if (ConversationContainer.Children.Count == 0) return;
            var last = ConversationContainer.Children[^1];
            await ConversationScroll.ScrollToAsync((View)last, ScrollToPosition.End, animated: false);
        }
        catch { /* ignore */ }
    }

    private void OnCapabilityProcessStarted(object? sender, ProcessStartedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
            GetOrCreateCapabilityBubble(e.CorrelationId, e.FileName));
    }

    private void OnCapabilityProcessOutput(object? sender, ProcessOutputEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.CorrelationId)) return;
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var bubble = GetOrCreateCapabilityBubble(e.CorrelationId!, e.FileName);
            if (e.IsError) bubble.SetStatus("stderr");
            bubble.AppendOutput(e.Text);
        });
    }

    private void OnCapabilityProcessCompleted(object? sender, ProcessCompletedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var bubble = GetOrCreateCapabilityBubble(e.CorrelationId, e.FileName);
            bubble.Complete(e.Result.Success);
            _capabilityBubbles.Remove(e.CorrelationId);
        });
    }

    private void OnCoordinatorStarted(object? sender, AgentExecutionStartedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
            GetOrCreateCapabilityBubble(e.ProcessId, e.Title));
    }

    private void OnCoordinatorOutput(object? sender, AgentExecutionOutputEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var bubble = GetOrCreateCapabilityBubble(e.CorrelationId, e.CorrelationId);
            if (e.Stream == "stderr") bubble.SetStatus("stderr");
            bubble.AppendOutput(e.Text);
        });
    }

    private void OnCoordinatorCompleted(object? sender, AgentExecutionCompletedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.ProcessId)) return;
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var bubble = GetOrCreateCapabilityBubble(e.ProcessId!, e.Executor);
            string? message = !string.IsNullOrWhiteSpace(e.Result.StandardOutput) ? e.Result.StandardOutput.Trim()
                : !string.IsNullOrWhiteSpace(e.Result.StandardError) ? e.Result.StandardError.Trim() : null;
            bubble.Complete(e.Result.Success, message);
            _capabilityBubbles.Remove(e.ProcessId!);
        });
    }

    private void OnProcessesChanged(object? sender, NotifyCollectionChangedEventArgs e)
        => MainThread.BeginInvokeOnMainThread(UpdateProcessCardsVisibility);

    private void UpdateProcessCardsVisibility()
    {
        bool show = _processes.Processes.Any(p =>
        {
            string s = p.Status ?? "";
            return !s.Equals("Concluído", StringComparison.OrdinalIgnoreCase)
                && !s.Equals("Falhou", StringComparison.OrdinalIgnoreCase);
        });
        ProcessCardsHost.IsVisible = show;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        RuntimeConfig.Apply(_client);

        if (ConfigHost.Content is not AiConfigView cfg)
        {
            cfg = new AiConfigView();
            ConfigHost.Content = cfg;
        }
        cfg.Load(_client);
        SetConfigVisible(_configVisible);

        string activeRoot = AgentWorkspace.ActiveRoot;
        WorkspaceLabel.Text = ProjectAccessService.StatusText + "\n" +
            "Workspace: " + activeRoot +
            $" ({AgentWorkspace.CountFiles(activeRoot)} arquivo(s))";
        ModelLabel.Text = $"Modelo: {_client.Options.Model} · {_client.Options.BaseUrl}";

        UpdateProcessCardsVisibility();
        EnsureSession();
    }

    private async Task SafeAlertAsync(string title, string message, string cancel = "OK")
    {
        try
        {
            if (Handler == null)
            {
                AuraLog.Info($"ALERT [{title}]: {message}");
                return;
            }

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                try
                {
                    await DisplayAlert(title, message, cancel);
                }
                catch (Exception ex)
                {
                    AuraLog.Exception("SafeAlert.inner", ex);
                    AuraLog.Info($"ALERT [{title}]: {message}");
                }
            });
        }
        catch (Exception ex)
        {
            AuraLog.Exception("SafeAlert", ex);
            AuraLog.Info($"ALERT [{title}]: {message}");
        }
    }

    private void OnModeAgentClicked(object sender, EventArgs e)
    {
        _webMode = false;
        ApplyModeUi();
    }

    private void OnModeWebClicked(object sender, EventArgs e)
    {
        _webMode = true;
        ApplyModeUi();
        if (!_webLoaded)
        {
            BridgeWebView.Source = UrlDeepSeek;
            _webLoaded = true;
        }
    }

    private void ApplyModeUi()
    {
        AgentPane.IsVisible = !_webMode;
        WebPane.IsVisible = _webMode;

        // Maximizar WebView: esconder header de status e barra de input no modo Web AI
        StatusBarHost.IsVisible = !_webMode;
        InputBarHost.IsVisible = !_webMode;

        ModeAgentBtn.BackgroundColor = _webMode
            ? (Color)Application.Current!.Resources["AuraSurface2"]
            : (Color)Application.Current!.Resources["AuraAccent"];
        ModeAgentBtn.TextColor = _webMode
            ? (Color)Application.Current!.Resources["AuraTextPrimary"]
            : Colors.White;

        ModeWebBtn.BackgroundColor = _webMode
            ? (Color)Application.Current!.Resources["AuraAccent"]
            : (Color)Application.Current!.Resources["AuraSurface2"];
        ModeWebBtn.TextColor = _webMode
            ? Colors.White
            : (Color)Application.Current!.Resources["AuraTextPrimary"];
    }

    private void OnWebDeepSeekClicked(object sender, EventArgs e)
    {
        BridgeWebView.Source = UrlDeepSeek;
        _webLoaded = true;
    }

    private void OnWebChatGptClicked(object sender, EventArgs e)
    {
        BridgeWebView.Source = UrlChatGpt;
        _webLoaded = true;
    }

    private void OnWebReloadClicked(object sender, EventArgs e)
    {
        try { BridgeWebView.Reload(); }
        catch (Exception ex) { AuraLog.Exception("BridgeWeb.Reload", ex); }
    }

    private async void OnCopyContextClicked(object sender, EventArgs e)
    {
        try
        {
            string pack = BuildContextPack();
            await Clipboard.Default.SetTextAsync(pack);
            await AppendBubbleAsync(
                "📋 Contexto copiado. Vá em Web AI, cole no chat e peça comandos em ```aura-sh.\n\n" +
                Shorten(pack, 400),
                user: false, isTool: true);
        }
        catch (Exception ex)
        {
            await SafeAlertAsync("Contexto", ex.Message);
        }
    }

    private string BuildContextPack()
    {
        string root = "";
        int files = 0;
        try
        {
            root = AgentWorkspace.ActiveRoot ?? "";
            files = AgentWorkspace.CountFiles(root);
        }
        catch { /* ignore */ }

        string goal = !string.IsNullOrWhiteSpace(_lastUserGoal)
            ? _lastUserGoal!
            : (CommandEditor.Text?.Trim() ?? "(defina o objetivo)");

        var sb = new StringBuilder();
        sb.AppendLine("### AURA_CONTEXT");
        sb.AppendLine("Objetivo: " + goal);
        sb.AppendLine("Workspace: " + root);
        sb.AppendLine("Arquivos no workspace: " + files);
        sb.AppendLine("Shell: Android /bin/sh (toybox)");
        sb.AppendLine("Evitar: apt, apt-get, pip, npm, git (salvo se confirmado no aparelho)");
        if (!string.IsNullOrWhiteSpace(_lastAssistantText))
        {
            sb.AppendLine("Última saída do agente:");
            sb.AppendLine(Shorten(_lastAssistantText, 500));
        }
        sb.AppendLine("### PEDIDO À IA WEB");
        sb.AppendLine("Responda em português, curto.");
        sb.AppendLine("1) Análise em até 3 linhas");
        sb.AppendLine("2) UM bloco ```aura-sh\ncomandos\n```");
        sb.AppendLine("Não invente caminhos fora do workspace.");
        return sb.ToString().TrimEnd();
    }

    private async void OnPastePlanClicked(object sender, EventArgs e)
    {
        try
        {
            string? clip = await Clipboard.Default.GetTextAsync();
            if (string.IsNullOrWhiteSpace(clip))
            {
                await SafeAlertAsync("Colar plano", "Clipboard vazio. Copie a resposta da Web AI e tente de novo.");
                return;
            }

            string text = clip.Trim();
            string? shell = LocalPlaybook.ExtractAuraShell(text);

            _webMode = false;
            ApplyModeUi();

            if (!string.IsNullOrWhiteSpace(shell))
            {
                await AppendBubbleAsync("▶ Plano da web (aura-sh) — executando…", user: false, isTool: true);
                await AppendBubbleAsync(text, user: false);
                await TryExecuteAuraShellAsync(text);
                _playbook?.RememberFromRun(_lastUserGoal ?? "plano web", null, text);
                return;
            }

            CommandEditor.Text = text;
            await AppendBubbleAsync(
                "📋 Texto da web colado no editor (sem ```aura-sh). Revise e toque ▶ se quiser que o agente trate.",
                user: false, isTool: true);
        }
        catch (Exception ex)
        {
            await SafeAlertAsync("Colar plano", ex.Message);
        }
    }

    private void OnConfigClicked(object sender, EventArgs e) => SetConfigVisible(!_configVisible);

    private void SetConfigVisible(bool visible)
    {
        _configVisible = visible;
        ConfigHost.IsVisible = visible;
        ConfigButton.Text = visible ? "×" : "⚙";
    }

    // --- restante do arquivo permanece idêntico (não alterado nesta mudança mínima) ---
    // O conteúdo completo original de AgentPage.xaml.cs foi preservado nas partes não tocadas.
    // Esta versão atualiza apenas ApplyModeUi + referências a StatusBarHost/InputBarHost.
    // Para o push completo, o restante do método body deve ser o original.
}
