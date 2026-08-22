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
            "Você é o agente de arquivos da AURA, um assistente que trabalha " +
            "no diretório de trabalho da AURA. Quando houver um projeto vinculado, " +
            "o diretório é a própria pasta escolhida (acesso direto) ou uma cópia " +
            "de trabalho sincronizada. Você PODE listar, ler, criar, editar e " +
            "sobrescrever arquivos do diretório de trabalho e executar comandos shell " +
            "(sh -c) nesse local. Prefira ferramentas a respostas vagas: quando o " +
            "usuário pedir uma tarefa, use as ferramentas e confirme o que foi feito. " +
            "Responda em português, de forma curta e objetiva. Caminhos são sempre " +
            "relativos ao diretório de trabalho.";

        _session = new AgentSession(_client, tools, systemPrompt, memory: _memory);
        _session.Step += OnAgentStep;

        AppendBubble("Pronto. Posso trabalhar no workspace e acompanhar processos em tempo real. O que deseja fazer?", user: false);
    }

    private async void OnLinkProjectClicked(object sender, EventArgs e)
    {
        try
        {
            string projectPath = "/storage/emulated/0/Download/AURA/AURA_assistente";
            if (Directory.Exists(projectPath))
            {
                await DisplayAlert("Sucesso", $"Projeto vinculado: {projectPath}", "OK");
            }
            else
            {
                await DisplayAlert("Erro", $"Projeto não encontrado em: {projectPath}", "OK");
            }
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
        AppendBubble(text, user: true);
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
                AppendBubble(answer, user: false);
                _voice?.SetLastUtterance(answer);
                await SpeakAsync(answer);
                return;
            }

            _processes.Update(process.Id, "Executando", "Processando solicitação", 0.1);
            string answerFromAgent;
            if (string.IsNullOrWhiteSpace(RuntimeConfig.ApiKey) && string.IsNullOrWhiteSpace(_client.Options.ApiKey))
            {
                _processes.Update(process.Id, "Pesquisando", "Buscando na web", 0.35);
                AppendBubble("Buscando na web (Bing)...", user: false, isTool: true);
                answerFromAgent = await WebSearchAnswer.SearchWithRefinementAsync(text);
            }
            else
            {
                string? readyError = RuntimeConfig.EnsureReadyForRequest(_client);
                if (readyError != null)
                {
                    _processes.Fail(process.Id, readyError);
                    AppendBubble(readyError, user: false, isError: true);
                    return;
                }
                _session = null;
                EnsureSession();
                answerFromAgent = await _session!.RunAsync(text);
            }

            _processes.Complete(process.Id, "Resultado entregue");
            AppendBubble(answerFromAgent, user: false);
            _voice?.SetLastUtterance(answerFromAgent);
            await SpeakAsync(answerFromAgent);

            if (ProjectAccessService.IsLinked && !ProjectAccessService.IsDirect)
            {
                _processes.Update(process.Id, "Sincronizando", "Atualizando projeto", 0.9);
                int synced = await ProjectAccessService.SyncBackAsync();
                _processes.Complete(process.Id, $"Concluído · {synced} arquivo(s) sincronizado(s)");
                AppendBubble($"↥ Projeto sincronizado: {synced} arquivo(s) atualizado(s).", user: false, isTool: true);
            }
        }
        catch (Exception ex)
        {
            _processes.Fail(process.Id, ex.Message);
            AppendBubble("Erro: " + ex.Message, user: false, isError: true);
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
        AppendBubble("◆ " + step.ToolName + " " + argsPreview + "\n" + resultPreview, user: false, isTool: true);
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

    private void AppendBubble(string text, bool user, bool isTool = false, bool isError = false)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
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
                    Text = text,
                    FontSize = 13,
                    TextColor = user ? Color.FromArgb("#dfe7ff") : Color.FromArgb("#e8e8f0"),
                    LineBreakMode = LineBreakMode.WordWrap
                };

                if (!user && !isTool && !isError)
                {
                    var copyButton = new Button
                    {
                        Text = "Copiar",
                        FontSize = 10,
                        Padding = new Thickness(8, 3),
                        HeightRequest = 30,
                        HorizontalOptions = LayoutOptions.Start,
                        Style = (Style)Resources["BtnGhost"]
                    };
                    copyButton.Clicked += async (_, _) =>
                    {
                        try
                        {
                            await Clipboard.Default.SetTextAsync(text);
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
            }
            catch (Exception ex)
            {
                AuraLog.Exception("AgentPage.AppendBubble", ex);
            }
        });
    }

    private static string Shorten(string? text, int max)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        text = text.Replace("\r", " ").Replace("\n", " ").Trim();
        return text.Length <= max ? text : text[..max] + "…";
    }
}
