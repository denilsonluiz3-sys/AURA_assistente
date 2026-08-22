using AURA.AI;
using AURA.Agents;
using AURA.Core.Events;
using AURA.Memory;
using AURA.Mobile.Diagnostics;
using AURA.Mobile.Services;
using AURA.Mobile.Speech;
using AURA.Modules.Executors;
using AURA.Mobile.Controls;
using Microsoft.Maui.Controls.Shapes;

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
    private readonly SemaphoreSlim _bubbleGate = new(1, 1);
    private readonly List<string> _recentCommands = new();
    private AgentSession? _session;
    private string? _activeProcessId;
    private bool _configVisible;

    public AgentPage(OpenRouterClient client, MemoryStore memory, ISpeechService speech,
        ShellExecutor shell, ProcessRegistry processes, AuraOrchestrator orchestrator,
        VoiceAssistantService? voice = null)
    {
        InitializeComponent();
        _client = client;
        _memory = memory;
        _speech = speech;
        _shell = shell;
        _processes = processes;
        _orchestrator = orchestrator;
        ProcessCards.BindingContext = _processes;
        _voice = voice;
        LoadRecentsFromPrefs();
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

        EnsureSession();
    }

    private void OnConfigClicked(object sender, EventArgs e)
    {
        SetConfigVisible(!_configVisible);
    }

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

        string systemPrompt =
            "Você é o agente de arquivos da AURA. " +
            "Você TEM memória persistente: cada pergunta e resposta é gravada em memory.json " +
            "e reapresentada nas próximas sessões. Nunca diga que não tem memória. " +
            "Se o usuário pedir para 'criar memória' ou anotar algo, confirme que já está " +
            "sendo gravado automaticamente e, se quiser nota extra, use write_file em " +
            "memory-notes.md no workspace. " +
            "Você PODE listar, ler, criar, editar e sobrescrever arquivos do diretório de " +
            "trabalho e executar comandos shell (sh -c). Prefira ferramentas a respostas vagas. " +
            "Responda em português, curto e objetivo. Caminhos são relativos ao workspace. " +
            "Evite loops longos de ferramentas: se já tiver informação suficiente, responda em texto.";

        _session = new AgentSession(_client, tools, systemPrompt, memory: _memory);
        _session.Step += OnAgentStep;

        int memCount = 0;
        try { memCount = _memory.Read(tail: 64).Count; } catch { /* ignore */ }
        string welcome = memCount > 0
            ? $"Pronto. Memória ativa ({memCount} registro(s)). Use Histórico, Prompts ou digite uma instrução."
            : "Pronto. Memória ligada. Use ✦ Prompts, 🕑 Histórico ou digite uma instrução.";
        _ = AppendBubbleAsync(welcome, user: false);
    }

    // ── Projeto ──────────────────────────────────────────────────────────

    private async void OnProjectClicked(object sender, EventArgs e)
    {
        try
        {
            string root = AgentWorkspace.ActiveRoot;
            int files = AgentWorkspace.CountFiles(root);
            string status = ProjectAccessService.StatusText;
            string msg =
                $"Workspace:\n{root}\n\n" +
                $"Arquivos: {files}\n" +
                $"Status: {status}\n\n" +
                "Vincular pasta padrão em Download/AURA/AURA_assistente?";

            bool link = await DisplayAlert("Projeto", msg, "Vincular", "Fechar");
            if (!link)
                return;

            string projectPath = "/storage/emulated/0/Download/AURA/AURA_assistente";
            if (Directory.Exists(projectPath))
                await DisplayAlert("Projeto", $"Pasta encontrada:\n{projectPath}\n\nUse o workspace ativo ou configure o acesso SAF nas configurações.", "OK");
            else
                await DisplayAlert("Projeto", $"Pasta não encontrada:\n{projectPath}", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Erro", ex.Message, "OK");
            AuraLog.Exception("AgentPage.OnProjectClicked", ex);
        }
    }

    // ── Histórico (memória persistente) ─────────────────────────────────

    private async void OnHistoryClicked(object sender, EventArgs e)
    {
        try
        {
            var entries = _memory.Read(tail: 24);
            var turns = entries
                .Where(x => x.Kind == MemoryKind.Turn)
                .TakeLast(12)
                .ToList();

            if (turns.Count == 0)
            {
                await DisplayAlert("Histórico", "Ainda não há conversas gravadas na memória.", "OK");
                return;
            }

            var lines = new List<string>();
            foreach (var t in turns)
            {
                string role = string.IsNullOrWhiteSpace(t.Role) ? "?" : t.Role;
                string text = (t.Text ?? string.Empty).Replace('\n', ' ').Trim();
                if (text.Length > 90) text = text[..90] + "…";
                lines.Add($"[{role}] {text}");
            }

            string body = string.Join("\n\n", lines);
            bool reload = await DisplayAlert(
                "Conversas anteriores",
                body.Length > 1800 ? body[..1800] + "…" : body,
                "Recarregar no chat",
                "Fechar");

            if (!reload)
                return;

            ConversationContainer.Children.Clear();
            foreach (var t in turns)
            {
                bool isUser = string.Equals(t.Role, "user", StringComparison.OrdinalIgnoreCase);
                await AppendBubbleAsync(t.Text ?? string.Empty, user: isUser);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Histórico", "Falha ao ler memória: " + ex.Message, "OK");
            AuraLog.Exception("AgentPage.OnHistoryClicked", ex);
        }
    }

    // ── Recentes (comandos locais) ───────────────────────────────────────

    private void LoadRecentsFromPrefs()
    {
        try
        {
            string raw = Preferences.Default.Get("agent.recent_commands", string.Empty);
            if (string.IsNullOrWhiteSpace(raw))
                return;
            foreach (var line in raw.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (!_recentCommands.Contains(line))
                    _recentCommands.Add(line);
            }
        }
        catch { /* ignore */ }
    }

    private void SaveRecentsToPrefs()
    {
        try
        {
            Preferences.Default.Set("agent.recent_commands",
                string.Join('\n', _recentCommands.Take(20)));
        }
        catch { /* ignore */ }
    }

    private void RememberCommand(string text)
    {
        text = text.Trim();
        if (string.IsNullOrWhiteSpace(text))
            return;
        _recentCommands.RemoveAll(x => string.Equals(x, text, StringComparison.OrdinalIgnoreCase));
        _recentCommands.Insert(0, text);
        if (_recentCommands.Count > 20)
            _recentCommands.RemoveRange(20, _recentCommands.Count - 20);
        SaveRecentsToPrefs();
    }

    private async void OnRecentsClicked(object sender, EventArgs e)
    {
        if (_recentCommands.Count == 0)
        {
            await DisplayAlert("Recentes", "Nenhum comando recente ainda.", "OK");
            return;
        }

        string chosen = await DisplayActionSheet(
            "Comandos recentes",
            "Cancelar",
            null,
            _recentCommands.Take(10).ToArray());

        if (string.IsNullOrWhiteSpace(chosen) || chosen == "Cancelar")
            return;

        CommandEditor.Text = chosen;
    }

    // ── Prompts ──────────────────────────────────────────────────────────

    private async void OnPromptsClicked(object sender, EventArgs e)
    {
        try
        {
            var prompts = AgentPromptStore.LoadAll();
            if (prompts.Count == 0)
            {
                await DisplayAlert("Prompts", "Nenhum prompt disponível.", "OK");
                return;
            }

            string[] titles = prompts
                .Select(p => (p.BuiltIn ? "✦ " : "☆ ") + p.Title)
                .ToArray();

            string chosen = await DisplayActionSheet("Prompts prontos", "Cancelar", null, titles);
            if (string.IsNullOrWhiteSpace(chosen) || chosen == "Cancelar")
                return;

            int idx = Array.IndexOf(titles, chosen);
            if (idx < 0 || idx >= prompts.Count)
                return;

            var item = prompts[idx];
            string detail =
                (string.IsNullOrWhiteSpace(item.Description) ? "(sem descrição)" : item.Description)
                + "\n\n── Texto ──\n"
                + item.Body;

            bool use = await DisplayAlert(item.Title, detail, "Usar", "Fechar");
            if (!use)
                return;

            CommandEditor.Text = item.Body;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Prompts", ex.Message, "OK");
            AuraLog.Exception("AgentPage.OnPromptsClicked", ex);
        }
    }

    private async void OnAddPromptClicked(object sender, EventArgs e)
    {
        try
        {
            string title = await DisplayPromptAsync("Novo prompt", "Título:", "Salvar", "Cancelar", maxLength: 60);
            if (string.IsNullOrWhiteSpace(title))
                return;

            string description = await DisplayPromptAsync("Novo prompt", "Descrição curta:", "Salvar", "Cancelar", maxLength: 160)
                ?? string.Empty;

            string body = CommandEditor.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(body))
            {
                body = await DisplayPromptAsync("Novo prompt", "Texto do prompt:", "Salvar", "Cancelar", maxLength: 500)
                    ?? string.Empty;
            }

            if (string.IsNullOrWhiteSpace(body))
            {
                await DisplayAlert("Prompt", "Texto vazio — nada foi salvo.", "OK");
                return;
            }

            AgentPromptStore.AddCustom(title, description, body);
            await DisplayAlert("Prompt", $"Salvo: {title}", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Prompt", ex.Message, "OK");
            AuraLog.Exception("AgentPage.OnAddPromptClicked", ex);
        }
    }

    private async void OnClearChatClicked(object sender, EventArgs e)
    {
        bool ok = await DisplayAlert("Limpar chat", "Apagar bolhas desta tela? (a memória persistente permanece)", "Limpar", "Cancelar");
        if (!ok) return;
        ConversationContainer.Children.Clear();
        _session = null;
        EnsureSession();
    }

    // ── Run ──────────────────────────────────────────────────────────────

    private async void OnRunClicked(object sender, EventArgs e)
    {
        string text = CommandEditor.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text))
            return;

        RememberCommand(text);
        RuntimeConfig.Apply(_client);
        await AppendBubbleAsync(text, user: true);
        CommandEditor.Text = string.Empty;
        RunButton.IsEnabled = false;
        BusyIndicator.IsRunning = true;
        BusyIndicator.IsVisible = true;

        var process = _processes.Begin(text, "Assistente", "Entendendo solicitação");
        _activeProcessId = process.Id;

        try
        {
            if (ShouldOrchestrate(text))
            {
                _processes.Update(process.Id, "Planejando", "Orquestrador analisando a tarefa", 0.15);
                string answer = await _orchestrator.ExecuteAsync(text);
                _processes.Complete(process.Id, "Resultado entregue");
                await AppendBubbleAsync(answer, user: false);
                _voice?.SetLastUtterance(answer);
                await SpeakAsync(answer);
                return;
            }

            _processes.Update(process.Id, "Executando", "Processando solicitação", 0.1);
            string answerFromAgent;
            if (string.IsNullOrWhiteSpace(RuntimeConfig.ApiKey) && string.IsNullOrWhiteSpace(_client.Options.ApiKey))
            {
                _processes.Update(process.Id, "Pesquisando", "Buscando na web", 0.35);
                await AppendBubbleAsync("Buscando na web (Bing)...", user: false, isTool: true);
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
                _session = null;
                EnsureSession();
                answerFromAgent = await _session!.RunAsync(text);
            }

            _processes.Complete(process.Id, "Resultado entregue");
            await AppendBubbleAsync(answerFromAgent, user: false);
            _voice?.SetLastUtterance(answerFromAgent);
            await SpeakAsync(answerFromAgent);

            if (ProjectAccessService.IsLinked && !ProjectAccessService.IsDirect)
            {
                _processes.Update(process.Id, "Sincronizando", "Atualizando projeto", 0.9);
                int synced = await ProjectAccessService.SyncBackAsync();
                _processes.Complete(process.Id, $"Concluído · {synced} arquivo(s) sincronizado(s)");
                await AppendBubbleAsync($"↥ Projeto sincronizado: {synced} arquivo(s) atualizado(s).", user: false, isTool: true);
            }
        }
        catch (Exception ex)
        {
            string userMsg = FriendlyLlmError(ex);
            _processes.Fail(process.Id, userMsg);
            await AppendBubbleAsync("Erro: " + userMsg, user: false, isError: true);
            AuraLog.Exception("AgentPage.OnRunClicked", ex);
        }
        finally
        {
            if (_activeProcessId == process.Id)
                _activeProcessId = null;
            RunButton.IsEnabled = true;
            BusyIndicator.IsRunning = false;
            BusyIndicator.IsVisible = false;
        }
    }

    private static string FriendlyLlmError(Exception ex)
    {
        string m = ex.Message ?? string.Empty;
        if (m.Contains("401") || m.Contains("Unauthorized", StringComparison.OrdinalIgnoreCase))
            return "Chave de API inválida ou ausente. Configure na aba Assistente / Correções.";
        if (m.Contains("402") || m.Contains("PaymentRequired", StringComparison.OrdinalIgnoreCase) || m.Contains("credits", StringComparison.OrdinalIgnoreCase))
            return "Sem créditos no provedor LLM (ou max_tokens alto demais). Reduza tokens ou adicione créditos.";
        if (m.Contains("429") || m.Contains("TooManyRequests", StringComparison.OrdinalIgnoreCase))
            return "Limite de requisições (rate limit). Aguarde alguns segundos e tente de novo.";
        if (m.Contains("404") || m.Contains("NotFound", StringComparison.OrdinalIgnoreCase))
            return "Modelo ou endpoint não encontrado. Verifique o modelo configurado.";
        if (m.Contains("hostname") || m.Contains("nor servname") || m.Contains("Name or service not known", StringComparison.OrdinalIgnoreCase))
            return "Sem DNS/rede até o provedor LLM (openrouter.ai). Verifique Wi‑Fi/dados e tente de novo.";
        if (m.Contains("node already has a parent", StringComparison.OrdinalIgnoreCase))
            return "Erro interno ao montar a conversa com ferramentas. Atualize o APK e tente de novo.";
        if (m.Contains("limite de", StringComparison.OrdinalIgnoreCase) && m.Contains("ferramentas", StringComparison.OrdinalIgnoreCase))
            return "O agente usou demais as ferramentas sem fechar a resposta. Reformule o pedido de forma mais direta.";
        if (m.Length > 280)
            return m.Substring(0, 280) + "…";
        return m;
    }

    private static bool ShouldOrchestrate(string text)
    {
        string l = text.ToLowerInvariant();
        return l.Contains("orquestre") || l.Contains("orquestr") ||
               l.Contains("planeje") || l.Contains("divida em tarefas") ||
               l.Contains("coordene") || l.Contains("pesquise e execute") ||
               l.Contains("pesquise e depois") || l.Contains("execute e depois");
    }

    private void OnAgentStep(AURA.AI.AgentStep step)
    {
        string argsPreview = Shorten(step.Arguments, 70);
        string resultPreview = Shorten(step.Result, 140);
        if (!string.IsNullOrWhiteSpace(_activeProcessId))
            _processes.Update(_activeProcessId, "Executando", step.ToolName, 0.65);
        _ = AppendBubbleAsync("◆ " + step.ToolName + " " + argsPreview + "\n" + resultPreview, user: false, isTool: true);
    }

    private async void OnProcessCardClicked(object sender, EventArgs e)
    {
        if (sender is not Button button || button.BindingContext is not ProcessInfo process)
            return;

        if (Application.Current?.MainPage is MainPage main)
            await main.NavigateToProcessAsync(process.Target);
    }

    private async Task SpeakAsync(string text)
    {
        try
        {
            await _speech.InitializeAsync();
            await _speech.SpeakAsync(text);
        }
        catch (NotSupportedException)
        {
            AuraLog.Info("TTS: texto fora do alcance do motor atual, fala pulada.");
        }
    }

    private static Button CreateCopyButton()
    {
        return new Button
        {
            Text = "Copiar",
            FontSize = 10,
            Padding = new Thickness(8, 3),
            HeightRequest = 30,
            MinimumHeightRequest = 28,
            HorizontalOptions = LayoutOptions.Start,
            BackgroundColor = Colors.Transparent,
            TextColor = Color.FromArgb("#9aa3b5"),
            BorderColor = Color.FromArgb("#3a3a52"),
            BorderWidth = 1,
            CornerRadius = 8
        };
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
                Color background = user
                    ? Color.FromArgb("#1e2d54")
                    : isError
                        ? Color.FromArgb("#2a0f12")
                        : isTool
                            ? Color.FromArgb("#0f1420")
                            : Color.FromArgb("#13131d");

                Color stroke = user
                    ? Color.FromArgb("#2a3a6a")
                    : isError
                        ? Color.FromArgb("#5a1f24")
                        : Color.FromArgb("#242438");

                var border = new Border
                {
                    BackgroundColor = background,
                    Stroke = stroke,
                    StrokeThickness = 1,
                    Padding = new Thickness(10, 7),
                    StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(10) },
                    HorizontalOptions = user ? LayoutOptions.End : LayoutOptions.Start,
                    MaximumWidthRequest = 900
                };

                var messageLabel = new Label
                {
                    Text = text ?? string.Empty,
                    FontSize = 13,
                    TextColor = user
                        ? Color.FromArgb("#dfe7ff")
                        : isError
                            ? Color.FromArgb("#f0c0c4")
                            : Color.FromArgb("#e8e8f0"),
                    LineBreakMode = LineBreakMode.WordWrap
                };

                if (!user && !isTool && !isError)
                {
                    var copyButton = CreateCopyButton();
                    string payload = text ?? string.Empty;
                    copyButton.Clicked += async (_, _) =>
                    {
                        try
                        {
                            await Clipboard.Default.SetTextAsync(payload);
                            copyButton.Text = "Copiado";
                            await Task.Delay(900);
                            copyButton.Text = "Copiar";
                        }
                        catch (Exception ex)
                        {
                            AuraLog.Exception("AgentPage.CopyResponse", ex);
                        }
                    };

                    border.Content = new VerticalStackLayout
                    {
                        Spacing = 5,
                        Children = { messageLabel, copyButton }
                    };
                }
                else
                {
                    border.Content = messageLabel;
                }

                ConversationContainer.Children.Add(border);
                await ConversationScroll.ScrollToAsync(border, ScrollToPosition.End, true);
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            AuraLog.Exception("AgentPage.AppendBubbleAsync", ex);
            try
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    ConversationContainer.Children.Add(new Label
                    {
                        Text = text ?? string.Empty,
                        FontSize = 13,
                        TextColor = Color.FromArgb("#e8e8f0"),
                        Margin = new Thickness(14, 4)
                    });
                });
            }
            catch (Exception fallbackEx)
            {
                AuraLog.Exception("AgentPage.AppendBubbleAsync.Fallback", fallbackEx);
            }
        }
        finally
        {
            if (entered)
                _bubbleGate.Release();
        }
    }

    private static string Shorten(string? text, int max)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        text = text.Replace("\r", " ").Replace("\n", " ").Trim();
        return text.Length <= max ? text : text[..max] + "…";
    }
}
