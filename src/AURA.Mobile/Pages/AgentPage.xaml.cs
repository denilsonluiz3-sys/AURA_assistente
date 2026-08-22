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
            "Você PODE listar, ler, criar, editar e sobrescrever arquivos do diretório de " +
            "trabalho e executar comandos shell (sh -c). Prefira ferramentas a respostas vagas. " +
            "Responda em português, curto e objetivo. Caminhos são relativos ao workspace. " +
            "Evite loops longos de ferramentas: se já tiver informação suficiente, responda em texto.";

        _session = new AgentSession(_client, tools, systemPrompt, memory: _memory);
        _session.Step += OnAgentStep;

        int memCount = 0;
        try { memCount = _memory.Read(tail: 64).Count; } catch { /* ignore */ }
        string welcome = memCount > 0
            ? $"Pronto. Memória ativa ({memCount} registro(s)). O que deseja fazer?"
            : "Pronto. Memória persistente ligada. O que deseja fazer?";
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
            return "Sem créditos no provedor LLM.";
        if (m.Contains("429") || m.Contains("TooManyRequests", StringComparison.OrdinalIgnoreCase))
            return "Rate limit. Aguarde e tente de novo.";
        if (m.Contains("hostname") || m.Contains("nor servname"))
            return "Sem DNS/rede até o provedor LLM.";
        if (m.Contains("node already has a parent", StringComparison.OrdinalIgnoreCase))
            return "Erro interno de ferramentas. Atualize o APK.";
        if (m.Length > 280)
            return m.Substring(0, 280) + "…";
        return m;
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
            AuraLog.Info("TTS: fala pulada.");
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
            HorizontalOptions = LayoutOptions.Start,
            BackgroundColor = Colors.Transparent,
            TextColor = Color.FromArgb("#9aa3b5"),
            BorderColor = Color.FromArgb("#3a3a52"),
            BorderWidth = 1,
            CornerRadius = 8
        };
    }

    private void AttachLongPressCopy(View view, string text)
    {
        string payload = text ?? string.Empty;
        var longPress = new TapGestureRecognizer
        {
            NumberOfTapsRequired = 1,
            Buttons = ButtonsMask.Primary
        };
        // MAUI: Gesture LongPress via Pointer/Touch — usamos long press nativo
        var recognizer = new TapGestureRecognizer();
        // Fallback: double-tap também copia (funciona em todas as plataformas MAUI)
        var doubleTap = new TapGestureRecognizer { NumberOfTapsRequired = 2 };
        doubleTap.Tapped += async (_, _) =>
        {
            try
            {
                await Clipboard.Default.SetTextAsync(payload);
                await DisplayAlert("Copiado", "Texto da bolha copiado.", "OK");
            }
            catch (Exception ex)
            {
                AuraLog.Exception("AgentPage.DoubleTapCopy", ex);
            }
        };
        view.GestureRecognizers.Add(doubleTap);

#if ANDROID
        // Long-press nativo Android no ContentView wrapper
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
                        await MainThread.InvokeOnMainThreadAsync(async () =>
                            await DisplayAlert("Copiado", "Texto da bolha copiado.", "OK"));
                    }
                    catch (Exception ex)
                    {
                        AuraLog.Exception("AgentPage.LongPressCopy", ex);
                    }
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

                AttachLongPressCopy(border, text ?? string.Empty);

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
