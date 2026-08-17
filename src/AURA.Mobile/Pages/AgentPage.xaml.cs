using AURA.AI;
using AURA.Core.Events;
using AURA.Memory;
using AURA.Mobile.Diagnostics;
using AURA.Mobile.Speech;
using AURA.Modules.Executors;
using AURA.Mobile.Controls;

namespace AURA.Mobile.Pages;

public partial class AgentPage : ContentPage
{
    private readonly OpenRouterClient _client;
    private readonly MemoryStore _memory;
    private readonly ISpeechService _speech;
    private readonly VoiceAssistantService? _voice;
    private readonly ShellExecutor _shell;
    private readonly ProcessRegistry _processes;
    private AgentSession? _session;
    private string? _activeProcessId;

    public AgentPage(OpenRouterClient client, MemoryStore memory, ISpeechService speech,
        ShellExecutor shell, ProcessRegistry processes, VoiceAssistantService? voice = null)
    {
        InitializeComponent();
        _client = client;
        _memory = memory;
        _speech = speech;
        _shell = shell;
        _processes = processes;
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

        string workspace = AgentWorkspace.EnsureCreated();
        string activeRoot = AgentWorkspace.ActiveRoot;
        WorkspaceLabel.Text = ProjectAccessService.StatusText + "\n" +
            "Workspace: " + activeRoot +
            $" ({AgentWorkspace.CountFiles(activeRoot)} arquivo(s))";
        ModelLabel.Text = $"Modelo: {_client.Options.Model} · {_client.Options.BaseUrl}";

        EnsureSession();
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
            "de trabalho sincronizada. " +
            "Você PODE listar, ler, criar, editar e sobrescrever arquivos do " +
            "diretório de trabalho e executar comandos shell (sh -c) nesse local. " +
            "Prefira ferramentas a respostas vagas: quando o usuário pedir uma " +
            "tarefa, use as ferramentas e confirme o que foi feito. " +
            "Responda em português, de forma curta e objetiva. " +
            "Caminhos são sempre relativos ao diretório de trabalho.";

        _session = new AgentSession(_client, tools, systemPrompt, memory: _memory);
        _session.Step += OnAgentStep;

        AppendBubble("Pronto. Posso listar, ler, criar e editar arquivos do workspace e " +
            "rodar comandos shell. O que deseja fazer?", user: false);
    }

    private async void OnLinkProjectClicked(object sender, EventArgs e)
    {
        ProjectButton.IsEnabled = false;
        try
        {
            if (!StoragePermissionHelper.IsAllFilesAccessGranted())
            {
                bool openSettings = await DisplayAlert(
                    "Acesso direto ao projeto",
                    "Conceder \"Todos os arquivos\" permite a AURA trabalhar DIRETO " +
                    "na pasta escolhida (sem cópia local, sem sincronização). " +
                    "Sem isso, a AURA usa uma cópia privada e sincroniza ao final.",
                    "Conceder acesso", "Usar cópia local");
                if (openSettings)
                {
                    StoragePermissionHelper.RequestAllFilesAccess();
                    return;
                }
            }

            bool linked = await ProjectAccessService.LinkAsync();
            if (!linked)
                return;

            _session = null;
            WorkspaceLabel.Text = ProjectAccessService.StatusText + "\n" +
                "Workspace: " + AgentWorkspace.ActiveRoot +
                $" ({AgentWorkspace.CountFiles(AgentWorkspace.ActiveRoot)} arquivo(s))";

            EnsureSession();
            AppendBubble(
                ProjectAccessService.IsDirect
                    ? "Projeto vinculado em acesso direto. A AURA lista, lê e edita a pasta escolhida, sem cópia local."
                    : "Projeto vinculado. A AURA trabalha na cópia local e sincroniza as alterações de volta ao projeto após cada tarefa.",
                user: false);
        }
        catch (OperationCanceledException)
        {
            AppendBubble("Seleção de projeto cancelada.", user: false);
        }
        catch (Exception ex)
        {
            AppendBubble("Erro ao vincular projeto: " + ex.Message, user: false, isError: true);
            AuraLog.Exception("AgentPage.OnLinkProjectClicked", ex);
        }
        finally
        {
            ProjectButton.IsEnabled = true;
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
        var process = _processes.Begin(text, "Agente", "Preparando execução");
        _activeProcessId = process.Id;

        try
        {
            _processes.Update(process.Id, "Executando", "Processando solicitação", 0.1);
            string answer;
            if (string.IsNullOrWhiteSpace(RuntimeConfig.ApiKey) &&
                string.IsNullOrWhiteSpace(_client.Options.ApiKey))
            {
                _processes.Update(process.Id, "Pesquisando", "Buscando na web", 0.35);
                AppendBubble("Buscando na web (Bing)...", user: false, isTool: true);
                answer = await WebSearchAnswer.SearchWithRefinementAsync(text);
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
                answer = await _session!.RunAsync(text);
            }

            _processes.Complete(process.Id, "Resultado entregue");
            AppendBubble(answer, user: false);
            _voice?.SetLastUtterance(answer);
            await SpeakAsync(answer);

            if (ProjectAccessService.IsLinked && !ProjectAccessService.IsDirect)
            {
                _processes.Update(process.Id, "Sincronizando", "Atualizando projeto", 0.9);
                int synced = await ProjectAccessService.SyncBackAsync();
                _processes.Complete(process.Id, $"Concluído · {synced} arquivo(s) sincronizado(s)");
                AppendBubble($"↥ Projeto sincronizado: {synced} arquivo(s) atualizado(s).",
                    user: false, isTool: true);
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

    private void OnAgentStep(AURA.AI.AgentStep step)
    {
        string argsPreview = Shorten(step.Arguments, 70);
        string resultPreview = Shorten(step.Result, 140);
        if (!string.IsNullOrWhiteSpace(_activeProcessId))
            _processes.Update(_activeProcessId, "Executando", step.ToolName, 0.65);
        AppendBubble("◆ " + step.ToolName + " " + argsPreview + "\n" + resultPreview,
            user: false, isTool: true);
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

        border.Content = new Label
        {
            Text = text,
            FontSize = 13,
            TextColor = user ? Color.FromArgb("#dfe7ff") : Color.FromArgb("#e8e8f0"),
            LineBreakMode = LineBreakMode.WordWrap
        };

        ConversationContainer.Children.Add(border);
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try { await ConversationScroll.ScrollToAsync(border, ScrollToPosition.End, true); }
            catch { }
        });
    }

    private static string Shorten(string? text, int max)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        text = text.Replace("\r", " ").Replace("\n", " ").Trim();
        return text.Length <= max ? text : text[..max] + "…";
    }
}
