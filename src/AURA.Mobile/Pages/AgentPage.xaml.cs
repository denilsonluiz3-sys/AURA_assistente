using AURA.AI;
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
    private AgentSession? _session;

    public AgentPage(OpenRouterClient client, MemoryStore memory, ISpeechService speech,
        ShellExecutor shell, VoiceAssistantService? voice = null)
    {
        InitializeComponent();
        _client = client;
        _memory = memory;
        _speech = speech;
        _shell = shell;
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
        {
            return;
        }

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

        AppendBubble(
            "Pronto. Posso listar, ler, criar e editar arquivos do workspace e " +
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
                    ? "Projeto vinculado em acesso direto. A AURA lista, lê e edita " +
                      "a pasta escolhida, sem cópia local."
                    : "Projeto vinculado. A AURA trabalha na cópia local e sincroniza " +
                      "as alterações de volta ao projeto após cada tarefa.",
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
        {
            return;
        }

        RuntimeConfig.Apply(_client);
        AppendBubble(text, user: true);
        CommandEditor.Text = string.Empty;

        RunButton.IsEnabled = false;
        BusyIndicator.IsRunning = true;
        BusyIndicator.IsVisible = true;

        try
        {
            string answer;
            if (string.IsNullOrWhiteSpace(RuntimeConfig.ApiKey) &&
                string.IsNullOrWhiteSpace(_client.Options.ApiKey))
            {
                AppendBubble("Buscando na web (Bing)...", user: false, isTool: true);
                answer = await WebSearchAnswer.SearchWithRefinementAsync(text);
            }
            else
            {
                string? readyError = RuntimeConfig.EnsureReadyForRequest(_client);
                if (readyError != null)
                {
                    AppendBubble(readyError, user: false, isError: true);
                    return;
                }
                _session = null;
                EnsureSession();
                answer = await _session!.RunAsync(text);
            }
            AppendBubble(answer, user: false);
            _voice?.SetLastUtterance(answer);
            await SpeakAsync(answer);

            if (ProjectAccessService.IsLinked && !ProjectAccessService.IsDirect)
            {
                int synced = await ProjectAccessService.SyncBackAsync();
                AppendBubble($"↥ Projeto sincronizado: {synced} arquivo(s) atualizado(s).",
                    user: false, isTool: true);
            }
        }
        catch (Exception ex)
        {
            AppendBubble("Erro: " + ex.Message, user: false, isError: true);
            AuraLog.Exception("AgentPage.OnRunClicked", ex);
        }
        finally
        {
            RunButton.IsEnabled = true;
            BusyIndicator.IsRunning = false;
            BusyIndicator.IsVisible = false;
        }
    }

    private void OnAgentStep(AURA.AI.AgentStep step)
    {
        string argsPreview = Shorten(step.Arguments, 70);
        string resultPreview = Shorten(step.Result, 140);
        AppendBubble("◆ " + step.ToolName + " " + argsPreview + "\n" + resultPreview,
            user: false, isTool: true);
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

        LayoutOptions alignment = user ? LayoutOptions.End : LayoutOptions.Start;

        Color textColor = isError
            ? Color.FromArgb("#e05560")
            : isTool
                ? Color.FromArgb("#7a7a90")
                : Color.FromArgb("#e8e8f0");

        string display = text;

        var label = new Editor
        {
            Text = display,
            IsReadOnly = true,
            TextColor = textColor,
            FontSize = isTool ? 12 : 14,
            BackgroundColor = Colors.Transparent,
            AutoSize = Microsoft.Maui.Controls.EditorAutoSizeOption.TextChanges,
            MinimumHeightRequest = 24,
            Margin = new Thickness(-4, -6)
        };

        View bubbleContent = label;
        if (!user)
        {
            var copyButton = new Button
            {
                Text = "Copiar",
                BackgroundColor = Colors.Transparent,
                TextColor = Color.FromArgb("#7a7a90"),
                FontSize = 10,
                Padding = new Thickness(6, 0),
                HeightRequest = 24,
                HorizontalOptions = LayoutOptions.End
            };
            copyButton.Clicked += async (_, _) =>
            {
                await Clipboard.Default.SetTextAsync(display);
                string original = copyButton.Text;
                copyButton.Text = "✓";
                await Task.Delay(1500);
                copyButton.Text = original;
            };
            bubbleContent = new VerticalStackLayout { label, copyButton };
        }

        var border = new Border
        {
            BackgroundColor = background,
            Stroke = stroke,
            StrokeThickness = 1,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 14 },
            Padding = new Thickness(12, 8),
            MaximumWidthRequest = 340,
            HorizontalOptions = alignment,
            Content = bubbleContent
        };

        MainThread.BeginInvokeOnMainThread(() =>
        {
            ConversationContainer.Add(border);
            Dispatcher.Dispatch(() =>
                ConversationScroll.ScrollToAsync(0, ConversationContainer.Height, true));
        });
    }

    private static string Shorten(string text, int max)
    {
        text ??= string.Empty;
        string oneLine = text.Replace("\r", " ").Replace("\n", " ").Trim();
        if (oneLine.Length <= max)
        {
            return oneLine;
        }

        return oneLine.Substring(0, max) + "…";
    }
}
