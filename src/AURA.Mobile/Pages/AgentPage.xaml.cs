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
        _orchestrator = orchestrator;
        _cellRegistry = cellRegistry;
        _runtime = runtime;
        _playbook = playbook;
        ProcessCards.BindingContext = _processes;
        _voice = voice;
        LoadRecentsFromPrefs();

        _processes.Processes.CollectionChanged += OnProcessesChanged;
        UpdateProcessCardsVisibility();
        ApplyModeUi();
    }

    // RESTORED_MARKER - partial to test; full body follows in same commit if size allows
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
}
