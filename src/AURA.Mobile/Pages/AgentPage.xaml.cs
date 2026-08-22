using AURA.AI;
using AURA.Agents;
using AURA.Core.Events;
using AURA.Memory;
using AURA.Mobile.Diagnostics;
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
            ? $"Pronto. Memória ativa ({memCount} registro(s)). Posso trabalhar no workspace. O que deseja fazer?"
            : "Pronto. Memória persistente ligada (ainda vazia). Posso trabalhar no workspace. O que deseja fazer?";
        _ = AppendBubbleAsync(welcome, user: false);
    }

    private async void OnLinkProjectClicked(object sender, EventArgs e)
    {
        try
        {
            string projectPath = "/storage/emulated/0/Download/AURA/AURA_assistente";
            if (Directory.Exists(projectPath))
                await DisplayAlert("Sucesso", $"Projeto vinculado: {projectPath}", "OK");
            else
                await DisplayAlert("Erro", $"Projeto não encontrado em: {projectPath}", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Erro", $"Falha ao vincular projeto: {ex.Message}", "OK");
        }
    }

    private async void OnRunClicked(object sender, EventArgs e)
    {
        string text = CommandEditor.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text))
            return;

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

    /// <summary>
    /// Estilo ghost inline — NÃO usa Resources["BtnGhost"] (não existe no
    /// ResourceDictionary da página e derrubava a bolha inteira).
    /// </summary>
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

                // Resposta do assistente (não tool / não erro): texto + Copiar
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
            // Último recurso: tenta mostrar só o texto, sem botão
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
