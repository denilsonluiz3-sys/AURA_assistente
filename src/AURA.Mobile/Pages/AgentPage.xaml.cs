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
        sb.AppendLine("2) UM bloco ```aura-sh com comandos seguros para o shell acima");
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

    private async void OnAgentMenuClicked(object? sender, EventArgs e)
    {
        try
        {
            string action = await DisplayActionSheetAsync(
                "⚡ AURA Agent", "Fechar", null,
                "📂 Workspace",
                "🔍 Diagnóstico",
                "🧠 Memória",
                "▶ Continuar",
                "🖥️ Shell",
                "📋 Células",
                "▶ Rodar programa",
                "🌐 Contexto para Web AI",
                "📋 Colar plano",
                "⚙ Configurar IA",
                "➕ Adicionar atalho"
            );

            switch (action)
            {
                case "📂 Workspace": OnChipWorkspace(sender, e); break;
                case "🔍 Diagnóstico": OnChipDiagnostic(sender, e); break;
                case "🧠 Memória": OnChipMemory(sender, e); break;
                case "▶ Continuar": OnChipContinue(sender, e); break;
                case "🖥️ Shell": OnChipShellSafe(sender, e); break;
                case "📋 Células": await OnCellsSubmenuAsync(); break;
                case "▶ Rodar programa": await OnRunProgramSubmenuAsync(); break;
                case "🌐 Contexto para Web AI": OnCopyContextClicked(sender, e); break;
                case "📋 Colar plano": OnPastePlanClicked(sender, e); break;
                case "⚙ Configurar IA": SetConfigVisible(true); break;
                case "➕ Adicionar atalho": _ = OnAddShortcutAsync(); break;
            }
        }
        catch (Exception ex)
        {
            AuraLog.Exception("AgentMenu", ex);
        }
    }

    private async Task OnCellsSubmenuAsync()
    {
        try
        {
            string action = await DisplayActionSheetAsync(
                "📋 Células", "Voltar", null,
                "Ver status",
                "Ver log",
                "Parar célula");

            switch (action)
            {
                case "Ver status":
                    CommandEditor.Text = "liste as células e mostre o status de cada uma";
                    OnRunClicked(this, EventArgs.Empty);
                    break;
                case "Ver log":
                    CommandEditor.Text = "mostre as últimas 30 linhas do log de todas as células";
                    OnRunClicked(this, EventArgs.Empty);
                    break;
                case "Parar célula":
                    CommandEditor.Text = "pare todas as células que estejam rodando";
                    OnRunClicked(this, EventArgs.Empty);
                    break;
            }
        }
        catch (Exception ex)
        {
            await SafeAlertAsync("Células", ex.Message);
        }
    }

    private async Task OnRunProgramSubmenuAsync()
    {
        try
        {
            if (_cellRegistry == null)
            {
                await DeliverErrorBubbleAsync("Células não disponíveis.");
                return;
            }

            var programs = _cellRegistry.All.ToList();
            if (programs.Count == 0)
            {
                await DeliverErrorBubbleAsync("Nenhum programa registrado.");
                return;
            }

            string[] names = programs.Select(p => p.Name).ToArray();
            string chosen = await DisplayActionSheetAsync(
                "▶ Rodar programa", " Cancelar", null, names);

            var program = programs.FirstOrDefault(p => p.Name == chosen);
            if (program != null)
            {
                CommandEditor.Text = "run_program " + program.Name;
                OnRunClicked(this, EventArgs.Empty);
            }
        }
        catch (Exception ex)
        {
            await SafeAlertAsync("Rodar programa", ex.Message);
        }
    }

    private async Task OnAddShortcutAsync()
    {
        try
        {
            string? name = await DisplayPromptAsync("Novo atalho", "Nome do atalho:",
                "Salvar", "Cancelar", maxLength: 60);
            if (string.IsNullOrWhiteSpace(name)) return;

            string? command = await DisplayPromptAsync("Comando", "Comando shell ou ação:",
                "ex.: getprop ro.product.model", " Salvar", " Cancelar", maxLength: 200);
            if (string.IsNullOrWhiteSpace(command)) return;

            _solutions?.Record(
                task: name,
                actionTaken: "```aura-sh\n" + command + "\n```",
                resultDetails: "atalho criado pelo usuário",
                success: true);

            await AppendBubbleAsync(
                "✅ Atalho '" + name + "' criado. Diga \"" + name + "\" para executar.",
                user: false);
        }
        catch (Exception ex)
        {
            await SafeAlertAsync("Atalho", ex.Message);
        }
    }

    private void OnAddShortcutClicked(object? sender, EventArgs e)
        => _ = OnAddShortcutAsync();

    private async Task DeliverErrorBubbleAsync(string message)
    {
        try
        {
            await AppendBubbleAsync(message, user: false, isError: true);
        }
        catch { /* ignore */ }
    }

    /// <summary>
    /// True se o cliente atual pode chamar LLM sem API key (Ollama / NeedsKey=false).
    /// </summary>
    private bool HasLocalLlmWithoutKey()
    {
        if (string.Equals(_client.Options.Provider, "ollama", StringComparison.OrdinalIgnoreCase))
            return !string.IsNullOrWhiteSpace(_client.Options.BaseUrl);

        ProviderInfo? p = ProviderCatalog.Find(RuntimeConfig.Provider)
            ?? ProviderCatalog.Find(_client.Options.Provider);
        return p != null && !p.NeedsKey && !string.IsNullOrWhiteSpace(_client.Options.BaseUrl);
    }

    private void EnsureSession()
    {
        if (_session != null)
            return;

        string root = AgentWorkspace.ActiveRoot;
        var tools = new List<AgentTool>
        {
            new ListDirTool(root),
            new ReadFileTool(root),
            new WriteFileTool(root),
            new EditFileTool(root),
            new ShellAgentTool(root, _shell),
            new WebFetchTool()
        };

        if (_solutions != null)
        {
            tools.Add(new SearchMemoryTool(_solutions));
            tools.Add(new SaveMemoryTool(_solutions));
        }
        tools.Add(new ConversationMemoryTool(_memory));
        tools.Add(new AgentBrowserTool());
        if (_git != null && _python != null && _node != null)
            tools.Add(new AgentExecutorTool(_shell, _git, _python, _node));
        if (_cellRegistry != null && _runtime != null)
        {
            tools.Add(new AgentListProgramsTool(_cellRegistry));
            tools.Add(new AgentRunProgramTool(_cellRegistry, _runtime));
        }
        if (_android != null)
            tools.Add(new AndroidCapabilityTool(_android));

        string systemPrompt =
            "Você é o agente de arquivos e execução da AURA no Android. " +
            "REGRAS DE USO: use as ferramentas listadas em FERRAMENTAS REGISTRADAS em vez de inventar comandos ou supor capacidades. " +
            "A lista de ferramentas é gerada automaticamente — não precisa memorizá-la. " +
            "Quando precisar de uma capacidade, escolha a ferramenta mais específica e chame-a com os parâmetros corretos. " +
            "Se uma ferramenta falhar, não repita a mesma ação; tente uma alternativa ou reporte o erro. " +
            "ANDROID: use android(action=...) para battery, light, accelerometer, gyroscope, magnetometer, location, camera, audio, bluetooth, clipboard, clipboard_set, notification, vibrate, network, device, apps, app_list, app_find, app_launch, properties, memory, storage, all. " +
            "app_list retorna catálogo completo; app_find(text=nome) busca por nome; app_launch(package=...) abre um app pelo pacote. " +
            "Se um app não estiver na lista, use app_find com parte do nome antes de dizer que não está instalado. " +
            "MENU RÁPIDO: use o botão ⚡ no header para acessar Workspace, Diagnóstico, Memória, Células, Web AI e Adicionar atalhos sem digitar. " +
            "CONTINUIDADE: se a conversa já tem resultados de ferramentas, CONTINUE de onde parou — não reinicie a tarefa do zero. " +
            "Antes de inventar comandos, use search_memory para ver se já existe ação executável salva. " +
            "SHELL REALISTA: o shell é /bin/sh do Android (toybox). NÃO existe apt, apt-get, yum, pip, npm, node, python3 completo, git (salvo se um teste anterior provar o contrário). " +
            "Se um comando falhar com 'not found' / 'No such file', NÃO tente instalar o pacote nem repita a mesma família de comandos. Use alternativa com ls, cat, grep, sed, find, sh, ou as ferramentas de arquivo. " +
            "Prefira list_dir/read_file/write_file a shell quando for só ler/escrever arquivos. " +
            "Você TEM memória persistente de AÇÕES (comandos), não só de texto. " +
            "Use search_memory antes de repetir uma tarefa já resolvida. Use memory_save ao concluir. " +
            "Use run_executor para git/python/node quando disponíveis. Use list_programs/run_program para automações. " +
            "Quando resolver com shell, inclua um bloco ```aura-sh\ncomandos\n``` para a memória poder reexecutar depois. " +
            "Responda em português, curto e objetivo. " +
            "Não invente caminhos fora do workspace. Use o mínimo de rodadas de ferramenta. " +
            "NÃO use busca na web nem diga que pesquisou na internet para perguntas simples — responda direto com o modelo local quando possível.";

        _session = new AgentSession(_client, tools, systemPrompt, memory: _memory);
        _session.Step += OnAgentStep;

        int memCount = 0;
        try { memCount = _memory.Read(tail: 64).Count; } catch { /* ignore */ }
        string welcome = memCount > 0
            ? $"Pronto. Memória ({memCount}). Atalhos: ls · diagnóstico · memória X · continue. Chips na barra."
            : "Pronto. Atalhos: ls · diagnóstico · memória X · continue. 📋 Contexto → Web AI → ▶ Colar plano.";
        _ = AppendBubbleAsync(welcome, user: false);
    }

    private async void OnProjectClicked(object sender, EventArgs e)
    {
        try
        {
            string root = AgentWorkspace.ActiveRoot;
            int files = AgentWorkspace.CountFiles(root);
            string msg = $"Workspace:\n{root}\n\nArquivos: {files}\n{ProjectAccessService.StatusText}";
            await SafeAlertAsync("Projeto", msg);
        }
        catch (Exception ex)
        {
            await SafeAlertAsync("Erro", ex.Message);
        }
    }

    private async void OnHistoryClicked(object sender, EventArgs e)
    {
        try
        {
            var turns = _memory.Read(tail: 24).Where(x => x.Kind == MemoryKind.Turn).TakeLast(12).ToList();
            if (turns.Count == 0)
            {
                await SafeAlertAsync("Histórico", "Sem conversas gravadas.");
                return;
            }
            var lines = turns.Select(t =>
            {
                string text = (t.Text ?? "").Replace('\n', ' ');
                if (text.Length > 90) text = text[..90] + "…";
                return $"[{t.Role ?? "?"}] {text}";
            });
            string body = string.Join("\n\n", lines);
            bool reload = false;
            try
            {
                reload = await DisplayAlert("Conversas", body.Length > 1800 ? body[..1800] + "…" : body, "Recarregar", "Fechar");
            }
            catch (Exception ex)
            {
                AuraLog.Exception("OnHistory.DisplayAlert", ex);
            }
            if (!reload)
                return;
            ConversationContainer.Children.Clear();
            foreach (var t in turns)
                await AppendBubbleAsync(t.Text ?? "", user: string.Equals(t.Role, "user", StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception ex)
        {
            await SafeAlertAsync("Histórico", ex.Message);
        }
    }

    private void LoadRecentsFromPrefs()
    {
        try
        {
            foreach (var line in Preferences.Default.Get("agent.recent_commands", "")
                         .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                if (!_recentCommands.Contains(line)) _recentCommands.Add(line);
        }
        catch { }
    }

    private void SaveRecentsToPrefs()
    {
        try { Preferences.Default.Set("agent.recent_commands", string.Join('\n', _recentCommands.Take(20))); }
        catch { }
    }

    private void RememberCommand(string text)
    {
        text = text.Trim();
        if (string.IsNullOrWhiteSpace(text)) return;
        _recentCommands.RemoveAll(x => string.Equals(x, text, StringComparison.OrdinalIgnoreCase));
        _recentCommands.Insert(0, text);
        while (_recentCommands.Count > 20) _recentCommands.RemoveAt(_recentCommands.Count - 1);
        SaveRecentsToPrefs();
    }

    private async void OnRecentsClicked(object sender, EventArgs e)
    {
        if (_recentCommands.Count == 0) { await SafeAlertAsync("Recentes", "Nenhum ainda."); return; }
        string chosen = await DisplayActionSheet("Recentes", "Cancelar", null, _recentCommands.Take(10).ToArray());
        if (!string.IsNullOrWhiteSpace(chosen) && chosen != "Cancelar") CommandEditor.Text = chosen;
    }

    private async void OnPromptsClicked(object sender, EventArgs e)
    {
        try
        {
            var prompts = AgentPromptStore.LoadAll();
            string[] titles = prompts.Select(p => (p.BuiltIn ? "✦ " : "☆ ") + p.Title).ToArray();
            string chosen = await DisplayActionSheet("Prompts", "Cancelar", null, titles);
            if (string.IsNullOrWhiteSpace(chosen) || chosen == "Cancelar") return;
            int idx = Array.IndexOf(titles, chosen);
            if (idx < 0) return;
            var item = prompts[idx];
            if (await DisplayAlert(item.Title, item.Description + "\n\n" + item.Body, "Usar", "Fechar"))
                CommandEditor.Text = item.Body;
        }
        catch (Exception ex) { await SafeAlertAsync("Prompts", ex.Message); }
    }

    private async void OnAddPromptClicked(object sender, EventArgs e)
    {
        string? title = await DisplayPromptAsync("Novo prompt", "Título:", "Salvar", "Cancelar", maxLength: 60);
        if (string.IsNullOrWhiteSpace(title)) return;
        string desc = await DisplayPromptAsync("Novo prompt", "Descrição:", "Salvar", "Cancelar", maxLength: 160) ?? "";
        string body = CommandEditor.Text?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(body))
            body = await DisplayPromptAsync("Novo prompt", "Texto:", "Salvar", "Cancelar", maxLength: 500) ?? "";
        if (string.IsNullOrWhiteSpace(body)) { await SafeAlertAsync("Prompt", "Vazio."); return; }
        AgentPromptStore.AddCustom(title, desc, body);
        await SafeAlertAsync("Prompt", "Salvo: " + title);
    }

    private async void OnClearChatClicked(object sender, EventArgs e)
    {
        try
        {
            if (!await DisplayAlert("Limpar", "Limpar bolhas e reiniciar sessão do agente?", "Limpar", "Cancelar")) return;
        }
        catch (Exception ex)
        {
            AuraLog.Exception("OnClearChat", ex);
            return;
        }
        ConversationContainer.Children.Clear();
        _session = null;
        EnsureSession();
    }

    private void OnChipWorkspace(object sender, EventArgs e)
    {
        CommandEditor.Text = "listar workspace";
        OnRunClicked(RunButton, e);
    }

    private void OnChipDiagnostic(object sender, EventArgs e)
    {
        CommandEditor.Text = "diagnóstico";
        OnRunClicked(RunButton, e);
    }

    private async void OnChipMemory(object sender, EventArgs e)
    {
        try
        {
            string? topic = await DisplayPromptAsync("Memória", "Buscar ação salva sobre:", "Buscar", "Cancelar", maxLength: 120);
            if (string.IsNullOrWhiteSpace(topic)) return;
            CommandEditor.Text = "memória " + topic.Trim();
            OnRunClicked(RunButton, e);
        }
        catch (Exception ex)
        {
            await SafeAlertAsync("Memória", ex.Message);
        }
    }

    private void OnChipContinue(object sender, EventArgs e)
    {
        CommandEditor.Text = "continue";
        OnRunClicked(RunButton, e);
    }

    private void OnChipShellSafe(object sender, EventArgs e)
    {
        CommandEditor.Text =
            "Só use run_shell com comandos toybox (ls, cat, grep, find, sed, pwd, df, getprop). " +
            "Proibido apt, pip, npm, git install. Se faltar comando, diga e pare.";
    }

    private static bool IsContinueCommand(string text)
    {
        string l = text.Trim().ToLowerInvariant();
        return l is "continue" or "continua" or "continuar" or "prosseguir";
    }

    private string ExpandContinueCommand(string text)
    {
        if (!IsContinueCommand(text))
            return text;

        if (string.IsNullOrWhiteSpace(_lastUserGoal))
            return "continue de onde parou; não reinicie a tarefa do zero";

        return "continue de onde parou; não reinicie. Objetivo anterior: " + _lastUserGoal;
    }

    private void OnEditorCompleted(object? sender, EventArgs e) => OnRunClicked(sender ?? RunButton, e);

    private async void OnRunClicked(object? sender, EventArgs e)
    {
        if (_runInFlight)
            return;

        try { CommandEditor.Unfocus(); } catch { /* ignore */ }

        string text = CommandEditor.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text))
        {
            await SafeAlertAsync("Agente", "Digite uma instrução antes de enviar.");
            return;
        }

        bool wasContinue = IsContinueCommand(text);
        string resolved = ExpandContinueCommand(text);

        _runInFlight = true;
        string? processId = null;
        _runShellCommands.Clear();
        if (!wasContinue)
            _lastUserGoal = text;

        if (_webMode)
        {
            _webMode = false;
            ApplyModeUi();
        }

        try
        {
            RememberCommand(wasContinue ? resolved : text);
            RuntimeConfig.Apply(_client);

            RunButton.IsEnabled = false;
            BusyIndicator.IsRunning = true;
            BusyIndicator.IsVisible = true;

            await AppendBubbleAsync(wasContinue ? resolved : text, user: true);
            CommandEditor.Text = string.Empty;

            var process = _processes.Begin(Shorten(resolved, 40), "Assistente", "Entendendo solicitação");
            processId = process.Id;
            _activeProcessId = process.Id;

            string playbookQuery = wasContinue ? resolved : text;
            bool isRepeat = !wasContinue && _lastUserQuery != null
                && string.Equals(text, _lastUserQuery, StringComparison.OrdinalIgnoreCase);

            if (isRepeat)
                _lastUserQuery = text;

            string? local = isRepeat ? null : _playbook?.TryResolveWithoutLlm(playbookQuery);
            if (!string.IsNullOrWhiteSpace(local))
            {
                _processes.Update(process.Id, "Playbook", "Ação local", 0.5);
                await DeliverAnswerAsync(local, process.Id, "Memória procedural");
                return;
            }

            if (!isRepeat && ShouldOrchestrate(resolved))
            {
                _processes.Update(process.Id, "Planejando", "Orquestrador", 0.15);
                string answer = await _orchestrator.ExecuteAsync(resolved);
                _playbook?.RememberFromRun(resolved, _runShellCommands, answer);
                await DeliverAnswerAsync(answer, process.Id, "OK");
                return;
            }

            _processes.Update(process.Id, "Executando", "Processando", 0.1);
            string answerFromAgent;

            bool hasCloudKey = !string.IsNullOrWhiteSpace(RuntimeConfig.ApiKey)
                || !string.IsNullOrWhiteSpace(_client.Options.ApiKey);
            bool hasLocalLlm = HasLocalLlmWithoutKey();

            // Só busca web se NÃO houver LLM configurado (nem nuvem nem Ollama)
            if (!hasCloudKey && !hasLocalLlm)
            {
                await AppendBubbleAsync("Sem LLM local/chave — tentando web…", user: false, isTool: true);
                answerFromAgent = await WebSearchAnswer.SearchWithRefinementAsync(resolved);
            }
            else
            {
                string? readyError = RuntimeConfig.EnsureReadyForRequest(_client);
                if (readyError != null)
                {
                    _processes.Fail(process.Id, readyError);
                    await AppendBubbleAsync(readyError, user: false, isError: true);
                    return;
                }

                // Sessão antiga pode ter sido criada antes de mudar o provedor
                _session = null;
                EnsureSession();
                answerFromAgent = await _session!.RunAsync(resolved);
            }

            _playbook?.RememberFromRun(resolved, _runShellCommands, answerFromAgent);
            await DeliverAnswerAsync(answerFromAgent, process.Id, "Resultado entregue");
            _lastUserQuery = text;

            if (ProjectAccessService.IsLinked && !ProjectAccessService.IsDirect)
            {
                int synced = await ProjectAccessService.SyncBackAsync();
                await AppendBubbleAsync($"↥ Sync: {synced} arquivo(s).", user: false, isTool: true);
            }
        }
        catch (AgentLlmException ex)
        {
            string userMsg = FriendlyLlmError(ex);
            if (!string.IsNullOrEmpty(processId))
                _processes.Fail(processId, userMsg);
            await AppendBubbleAsync("Erro: " + userMsg, user: false, isError: true);
            AuraLog.Exception("AgentPage.OnRunClicked", ex);
        }
        catch (Exception ex)
        {
            string userMsg = FriendlyLlmError(ex);
            if (!string.IsNullOrEmpty(processId))
                _processes.Fail(processId, userMsg);
            await AppendBubbleAsync("Erro: " + userMsg, user: false, isError: true);
            AuraLog.Exception("AgentPage.OnRunClicked", ex);
        }
        finally
        {
            if (_activeProcessId == processId) _activeProcessId = null;
            RunButton.IsEnabled = true;
            BusyIndicator.IsRunning = false;
            BusyIndicator.IsVisible = false;
            _runInFlight = false;
        }
    }

    private async Task DeliverAnswerAsync(string answer, string processId, string completeMessage)
    {
        string text = string.IsNullOrWhiteSpace(answer) ? "(sem texto na resposta)" : answer.Trim();
        _lastAssistantText = text;

        await AppendBubbleAsync(text, user: false);
        await TryExecuteAuraShellAsync(text);

        _processes.Complete(processId, completeMessage);
        _voice?.SetLastUtterance(text);

        await SpeakAsync(text);
    }

    private async Task TryExecuteAuraShellAsync(string answer)
    {
        string? script = LocalPlaybook.ExtractAuraShell(answer);
        if (string.IsNullOrWhiteSpace(script))
            return;

        await AppendBubbleAsync("▶ Executando aura-sh…", user: false, isTool: true);
        try
        {
            var req = new ExecutionRequest
            {
                Command = script,
                WorkingDirectory = AgentWorkspace.ActiveRoot,
                Timeout = TimeSpan.FromSeconds(60)
            };
            ExecutionResult result = await _shell.ExecuteAsync(req);
            string outText = result.CombineOutput();
            if (outText.Length > 2500) outText = outText[..2500] + "…";
            string status = result.Success ? "OK" : "FALHA";
            await AppendBubbleAsync($"[{status} exit={result.ExitCode}]\n{outText}", user: false, isTool: true);
        }
        catch (Exception ex)
        {
            await AppendBubbleAsync("Shell: " + ex.Message, user: false, isError: true);
            AuraLog.Exception("AgentPage.aura-sh", ex);
        }
    }

    private static string FriendlyLlmError(Exception ex)
    {
        if (ex is AgentLlmException aex)
        {
            return aex.ErrorKind switch
            {
                AgentErrorKind.RateLimited =>
                    "Rate limit atingido. 💡 Use a aba Web AI ou copie o contexto (📋) e cole no ChatGPT/DeepSeek.",
                AgentErrorKind.PaymentRequired =>
                    "Sem créditos LLM. Use Web AI (📋 Contexto) ou reduza max_tokens.",
                AgentErrorKind.InvalidApiKey =>
                    "API key inválida. Toque ⚙ na aba Assistente e corrija.",
                AgentErrorKind.Timeout =>
                    "A chamada LLM expirou. Tente novamente.",
                AgentErrorKind.Network =>
                    "Falha de rede ao falar com o provedor.",
                AgentErrorKind.InvalidRequest =>
                    "Requisição inválida para o modelo.",
                AgentErrorKind.ProviderError =>
                    "Erro no provedor LLM. Tente novamente.",
                _ => aex.Message.Length > 280 ? aex.Message[..280] + "…" : aex.Message
            };
        }

        string m = ex.Message ?? "";
        if (m.Contains("401")) return "API key inválida.";
        if (m.Contains("402")) return "Sem créditos LLM. Use Web AI (📋 Contexto) ou reduza max_tokens.";
        if (m.Contains("429")) return "Rate limit.";
        if (m.Contains("node already has a parent", StringComparison.OrdinalIgnoreCase))
            return "Erro interno de tools. Atualize o APK.";
        if (m.Contains("Invalid JSON in tool call", StringComparison.OrdinalIgnoreCase)
            || m.Contains("tool arguments must be", StringComparison.OrdinalIgnoreCase))
            return "Argumentos de ferramenta inválidos (JSON). Reformule o pedido.";
        return m.Length > 280 ? m[..280] + "…" : m;
    }

    private static bool ShouldOrchestrate(string text)
    {
        string l = text.ToLowerInvariant();
        return l.Contains("orquestre") || l.Contains("orquestr") ||
               l.Contains("planeje") || l.Contains("divida em tarefas") ||
               l.Contains("coordene") || l.Contains("pesquise e execute");
    }

    private void OnAgentStep(AURA.AI.AgentStep step)
    {
        if (!string.IsNullOrWhiteSpace(_activeProcessId))
            _processes.Update(_activeProcessId, "Executando", step.ToolName, 0.65);

        if (string.Equals(step.ToolName, "run_shell", StringComparison.OrdinalIgnoreCase))
        {
            string? cmd = TryExtractShellCommand(step.Arguments);
            if (!string.IsNullOrWhiteSpace(cmd))
                _runShellCommands.Add(cmd);
        }

        _ = AppendBubbleAsync("◆ " + step.ToolName + " " + Shorten(step.Arguments, 70) + "\n" + Shorten(step.Result, 140),
            user: false, isTool: true);
    }

    private static string? TryExtractShellCommand(string? argumentsJson)
    {
        if (string.IsNullOrWhiteSpace(argumentsJson))
            return null;
        try
        {
            using var doc = JsonDocument.Parse(argumentsJson);
            if (doc.RootElement.TryGetProperty("command", out var c) &&
                c.ValueKind == JsonValueKind.String)
                return c.GetString();
        }
        catch { /* ignore */ }
        return null;
    }

    private async void OnProcessCardClicked(object sender, EventArgs e)
    {
        if (sender is not Button button || button.BindingContext is not ProcessInfo process) return;
        if (Application.Current?.MainPage is MainPage main)
            await main.NavigateToProcessAsync(process.Target);
    }

    private async Task SpeakAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;
        try { await _speech.InitializeAsync(); await _speech.SpeakAsync(text); }
        catch (NotSupportedException) { }
        catch (Exception ex) { AuraLog.Exception("AgentPage.Speak", ex); }
    }

    private void AttachSubtleCopy(View view, string text)
    {
        string payload = text ?? "";
        var doubleTap = new TapGestureRecognizer { NumberOfTapsRequired = 2 };
        doubleTap.Tapped += async (_, _) =>
        {
            try
            {
                await Clipboard.Default.SetTextAsync(payload);
                AuraLog.Info("AgentPage: texto copiado (duplo toque)");
            }
            catch (Exception ex) { AuraLog.Exception("Copy", ex); }
        };
        view.GestureRecognizers.Add(doubleTap);
#if ANDROID
        view.HandlerChanged += (_, _) =>
        {
            if (view.Handler?.PlatformView is Android.Views.View native)
            {
                native.LongClickable = true;
                native.LongClick += async (_, args) =>
                {
                    args.Handled = true;
                    try
                    {
                        await Clipboard.Default.SetTextAsync(payload);
                        AuraLog.Info("AgentPage: texto copiado (long-press)");
                    }
                    catch (Exception ex) { AuraLog.Exception("LongPress", ex); }
                };
            }
        };
#endif
    }

    private async Task AppendBubbleAsync(string text, bool user, bool isTool = false, bool isError = false)
    {
        bool entered = false;
        try
        {
            await _bubbleGate.WaitAsync().ConfigureAwait(false);
            entered = true;
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                Color background = user ? Color.FromArgb("#1e2d54")
                    : isError ? Color.FromArgb("#2a0f12")
                    : isTool ? Color.FromArgb("#0f1420") : Color.FromArgb("#13131d");
                Color stroke = user ? Color.FromArgb("#2a3a6a")
                    : isError ? Color.FromArgb("#5a1f24") : Color.FromArgb("#242438");

                double maxW = 0;
                try
                {
                    var w = Width > 0 ? Width : DeviceDisplay.MainDisplayInfo.Width / DeviceDisplay.MainDisplayInfo.Density;
                    maxW = Math.Max(200, w * 0.88);
                }
                catch { maxW = 320; }

                var border = new Border
                {
                    BackgroundColor = background,
                    Stroke = stroke,
                    StrokeThickness = 1,
                    Padding = new Thickness(10, 7),
                    StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(10) },
                    HorizontalOptions = user ? LayoutOptions.End : LayoutOptions.Start,
                    MaximumWidthRequest = maxW
                };

                var messageLabel = new Label
                {
                    Text = text ?? "",
                    FontSize = 13,
                    TextColor = user ? Color.FromArgb("#dfe7ff") : isError ? Color.FromArgb("#f0c0c4") : Color.FromArgb("#e8e8f0"),
                    LineBreakMode = LineBreakMode.WordWrap
                };

                var copyBtn = new Button
                {
                    Text = "📋",
                    FontSize = 11,
                    Padding = new Thickness(6, 2),
                    HeightRequest = 28,
                    WidthRequest = 36,
                    BackgroundColor = Colors.Transparent,
                    TextColor = Color.FromArgb("#8a9bb8"),
                    HorizontalOptions = LayoutOptions.End
                };
                string payload = text ?? "";
                copyBtn.Clicked += async (_, _) =>
                {
                    try
                    {
                        await Clipboard.Default.SetTextAsync(payload);
                        AuraLog.Info("AgentPage: texto copiado (botão)");
                    }
                    catch (Exception ex) { AuraLog.Exception("CopyBtn", ex); }
                };

                var stack = new VerticalStackLayout { Spacing = 4 };
                stack.Children.Add(messageLabel);
                stack.Children.Add(copyBtn);
                border.Content = stack;

                AttachSubtleCopy(border, payload);
                ConversationContainer.Children.Add(border);

                await Task.Delay(40);
                try
                {
                    await ConversationScroll.ScrollToAsync(border, ScrollToPosition.End, animated: false);
                }
                catch
                {
                    try
                    {
                        double y = Math.Max(0, ConversationContainer.Height - ConversationScroll.Height);
                        await ConversationScroll.ScrollToAsync(0, y, animated: false);
                    }
                    catch { /* ignore */ }
                }
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            AuraLog.Exception("AppendBubbleAsync", ex);
        }
        finally
        {
            if (entered) _bubbleGate.Release();
        }
    }

    private static string Shorten(string? text, int max)
    {
        if (string.IsNullOrWhiteSpace(text)) return "";
        text = text.Replace("\r", " ").Replace("\n", " ").Trim();
        return text.Length <= max ? text : text[..max] + "…";
    }
}
