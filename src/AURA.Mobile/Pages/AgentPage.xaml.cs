using AURA.AI;
using AURA.Agents;
using AURA.Abstractions.Execution;
using AURA.Core.Events;
using AURA.Memory;
using AURA.Mobile.Diagnostics;
using AURA.Mobile.Services;
using AURA.Mobile.Speech;
using AURA.Modules.Executors;
using AURA.Mobile.Controls;
using Microsoft.Maui.Controls.Shapes;
using System.Collections.Specialized;

namespace AURA.Mobile.Pages;

public partial class AgentPage : ContentPage
{
    private readonly OpenRouterClient _client;
    private readonly MemoryStore _memory;
    private readonly ISpeechService _speech;
    private readonly VoiceAssistantService? _voice;
    private readonly ShellExecutor _shell;
    private readonly ProcessRegistry _processes;
    private readonly AuraOrchestrator _orchestrator;
    private readonly LocalPlaybook? _playbook;
    private readonly SemaphoreSlim _bubbleGate = new(1, 1);
    private readonly List<string> _recentCommands = new();
    private AgentSession? _session;
    private string? _activeProcessId;
    private bool _configVisible;
    private bool _runInFlight;

    public AgentPage(OpenRouterClient client, MemoryStore memory, ISpeechService speech,
        ShellExecutor shell, ProcessRegistry processes, AuraOrchestrator orchestrator,
        LocalPlaybook? playbook = null, VoiceAssistantService? voice = null)
    {
        InitializeComponent();
        _client = client;
        _memory = memory;
        _speech = speech;
        _shell = shell;
        _processes = processes;
        _orchestrator = orchestrator;
        _playbook = playbook;
        ProcessCards.BindingContext = _processes;
        _voice = voice;
        LoadRecentsFromPrefs();

        _processes.Processes.CollectionChanged += OnProcessesChanged;
        UpdateProcessCardsVisibility();
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

    private void OnConfigClicked(object sender, EventArgs e) => SetConfigVisible(!_configVisible);

    private void SetConfigVisible(bool visible)
    {
        _configVisible = visible;
        ConfigHost.IsVisible = visible;
        ConfigButton.Text = visible ? "×" : "⚙";
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

        // Ambiente real: Android sandbox (sem root, sem apt/pip).
        // Continuidade: a mesma AgentSession é reutilizada entre mensagens.
        string systemPrompt =
            "Você é o agente de arquivos e execução da AURA no Android. " +
            "REGRA PRINCIPAL: use ferramentas (list_dir, read_file, write_file, edit_file, run_shell, web_fetch) em vez de inventar. " +
            "CONTINUIDADE: se a conversa já tem resultados de ferramentas, CONTINUE de onde parou — não reinicie a tarefa do zero. " +
            "SHELL REALISTA: o shell é /bin/sh do Android (toybox). NÃO existe apt, apt-get, yum, pip, npm, node, python3 completo, git (salvo se um teste anterior provar o contrário). " +
            "Se um comando falhar com 'not found' / 'No such file', NÃO tente instalar o pacote nem repita a mesma família de comandos. Use alternativa com ls, cat, grep, sed, find, sh, ou as ferramentas de arquivo. " +
            "Prefira list_dir/read_file/write_file a shell quando for só ler/escrever arquivos. " +
            "Você TEM memória persistente — nunca diga que não tem memória. " +
            "Responda em português, curto e objetivo. " +
            "Se precisar de um script shell no workspace, devolva UM bloco:\n" +
            "```aura-sh\ncomando1\ncomando2\n```\n" +
            "Não invente caminhos fora do workspace. Use o mínimo de rodadas de ferramenta.";

        _session = new AgentSession(_client, tools, systemPrompt, memory: _memory);
        _session.Step += OnAgentStep;

        int memCount = 0;
        try { memCount = _memory.Read(tail: 64).Count; } catch { /* ignore */ }
        string welcome = memCount > 0
            ? $"Pronto. Memória ({memCount}). Sessão contínua — ferramentas prioritárias."
            : "Pronto. Sessão contínua. Ferramentas prioritárias. Histórico / Prompts / digite.";
        _ = AppendBubbleAsync(welcome, user: false);
    }

    private async void OnProjectClicked(object sender, EventArgs e)
    {
        try
        {
            string root = AgentWorkspace.ActiveRoot;
            int files = AgentWorkspace.CountFiles(root);
            string msg = $"Workspace:\n{root}\n\nArquivos: {files}\n{ProjectAccessService.StatusText}";
            await DisplayAlert("Projeto", msg, "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Erro", ex.Message, "OK");
        }
    }

    private async void OnHistoryClicked(object sender, EventArgs e)
    {
        try
        {
            var turns = _memory.Read(tail: 24).Where(x => x.Kind == MemoryKind.Turn).TakeLast(12).ToList();
            if (turns.Count == 0)
            {
                await DisplayAlert("Histórico", "Sem conversas gravadas.", "OK");
                return;
            }
            var lines = turns.Select(t =>
            {
                string text = (t.Text ?? "").Replace('\n', ' ');
                if (text.Length > 90) text = text[..90] + "…";
                return $"[{t.Role ?? "?"}] {text}";
            });
            string body = string.Join("\n\n", lines);
            if (!await DisplayAlert("Conversas", body.Length > 1800 ? body[..1800] + "…" : body, "Recarregar", "Fechar"))
                return;
            ConversationContainer.Children.Clear();
            foreach (var t in turns)
                await AppendBubbleAsync(t.Text ?? "", user: string.Equals(t.Role, "user", StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception ex)
        {
            await DisplayAlert("Histórico", ex.Message, "OK");
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
        if (_recentCommands.Count == 0) { await DisplayAlert("Recentes", "Nenhum ainda.", "OK"); return; }
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
        catch (Exception ex) { await DisplayAlert("Prompts", ex.Message, "OK"); }
    }

    private async void OnAddPromptClicked(object sender, EventArgs e)
    {
        string? title = await DisplayPromptAsync("Novo prompt", "Título:", "Salvar", "Cancelar", maxLength: 60);
        if (string.IsNullOrWhiteSpace(title)) return;
        string desc = await DisplayPromptAsync("Novo prompt", "Descrição:", "Salvar", "Cancelar", maxLength: 160) ?? "";
        string body = CommandEditor.Text?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(body))
            body = await DisplayPromptAsync("Novo prompt", "Texto:", "Salvar", "Cancelar", maxLength: 500) ?? "";
        if (string.IsNullOrWhiteSpace(body)) { await DisplayAlert("Prompt", "Vazio.", "OK"); return; }
        AgentPromptStore.AddCustom(title, desc, body);
        await DisplayAlert("Prompt", "Salvo: " + title, "OK");
    }

    private async void OnClearChatClicked(object sender, EventArgs e)
    {
        if (!await DisplayAlert("Limpar", "Limpar bolhas e reiniciar sessão do agente?", "Limpar", "Cancelar")) return;
        ConversationContainer.Children.Clear();
        _session = null;
        EnsureSession();
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
            await DisplayAlert("Agente", "Digite uma instrução antes de enviar.", "OK");
            return;
        }

        _runInFlight = true;
        string? processId = null;

        try
        {
            RememberCommand(text);
            RuntimeConfig.Apply(_client);

            RunButton.IsEnabled = false;
            BusyIndicator.IsRunning = true;
            BusyIndicator.IsVisible = true;

            await AppendBubbleAsync(text, user: true);
            CommandEditor.Text = string.Empty;

            var process = _processes.Begin(Shorten(text, 40), "Assistente", "Entendendo solicitação");
            processId = process.Id;
            _activeProcessId = process.Id;

            // 1) Playbook local — sem IA
            string? local = _playbook?.TryResolveWithoutLlm(text);
            if (!string.IsNullOrWhiteSpace(local))
            {
                _processes.Update(process.Id, "Playbook", "Resposta local", 0.5);
                await DeliverAnswerAsync(local, process.Id, "Playbook local");
                return;
            }

            if (ShouldOrchestrate(text))
            {
                _processes.Update(process.Id, "Planejando", "Orquestrador", 0.15);
                string answer = await _orchestrator.ExecuteAsync(text);
                _playbook?.RememberSuccess(text, answer);
                await DeliverAnswerAsync(answer, process.Id, "OK");
                return;
            }

            _processes.Update(process.Id, "Executando", "Processando", 0.1);
            string answerFromAgent;
            if (string.IsNullOrWhiteSpace(RuntimeConfig.ApiKey) && string.IsNullOrWhiteSpace(_client.Options.ApiKey))
            {
                await AppendBubbleAsync("Buscando na web...", user: false, isTool: true);
                answerFromAgent = await WebSearchAnswer.SearchWithRefinementAsync(text);
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
                // IMPORTANTE: reutiliza a mesma sessão — não zera o histórico de tools
                EnsureSession();
                answerFromAgent = await _session!.RunAsync(text);
            }

            _playbook?.RememberSuccess(text, answerFromAgent);
            await DeliverAnswerAsync(answerFromAgent, process.Id, "Resultado entregue");

            if (ProjectAccessService.IsLinked && !ProjectAccessService.IsDirect)
            {
                int synced = await ProjectAccessService.SyncBackAsync();
                await AppendBubbleAsync($"↥ Sync: {synced} arquivo(s).", user: false, isTool: true);
            }
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
        string m = ex.Message ?? "";
        if (m.Contains("401")) return "API key inválida.";
        if (m.Contains("402")) return "Sem créditos LLM.";
        if (m.Contains("429")) return "Rate limit.";
        if (m.Contains("node already has a parent", StringComparison.OrdinalIgnoreCase))
            return "Erro interno de tools. Atualize o APK.";
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
        _ = AppendBubbleAsync("◆ " + step.ToolName + " " + Shorten(step.Arguments, 70) + "\n" + Shorten(step.Result, 140),
            user: false, isTool: true);
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
                border.Content = messageLabel;

                AttachSubtleCopy(border, text ?? "");
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
