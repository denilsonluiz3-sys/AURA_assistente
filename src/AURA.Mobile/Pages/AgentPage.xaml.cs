using AURA.AI;
using AURA.Memory;
using AURA.Mobile.Diagnostics;

namespace AURA.Mobile.Pages;

public partial class AgentPage : ContentPage
{
    private readonly OpenRouterClient _client;
    private readonly MemoryStore _memory;
    private AgentSession? _session;

    public AgentPage(OpenRouterClient client, MemoryStore memory)
    {
        InitializeComponent();
        _client = client;
        _memory = memory;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        RuntimeConfig.Apply(_client);

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
            new ShellAgentTool(root)
        };

        string systemPrompt =
            "Você é o agente de arquivos da AURA, um assistente que trabalha " +
            "dentro do workspace local da AURA. Quando houver um projeto vinculado, " +
            "esse workspace é uma cópia de trabalho sincronizada com a pasta escolhida. " +
            "Você PODE listar, ler, criar, editar e sobrescrever arquivos do " +
            "workspace e executar comandos shell (sh -c) nesse diretório. " +
            "Prefira ferramentas a respostas vagas: quando o usuário pedir uma " +
            "tarefa, use as ferramentas e confirme o que foi feito. " +
            "Responda em português, de forma curta e objetiva. " +
            "Caminhos são sempre relativos ao workspace.";

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
            bool linked = await ProjectAccessService.LinkAsync();
            if (!linked)
                return;

            // As ferramentas guardam a raiz no momento da criação da sessão.
            // Ao trocar o projeto, recriamos a sessão para apontar para a nova raiz.
            _session = null;
            WorkspaceLabel.Text = ProjectAccessService.StatusText + "\n" +
                "Workspace: " + AgentWorkspace.ActiveRoot +
                $" ({AgentWorkspace.CountFiles(AgentWorkspace.ActiveRoot)} arquivo(s))";

            EnsureSession();
            AppendBubble(
                "Projeto vinculado. A AURA trabalha na cópia local e sincroniza " +
                "as alterações de volta ao projeto após cada tarefa.", user: false);
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

        EnsureSession();
        AppendBubble(text, user: true);
        CommandEditor.Text = string.Empty;

        RunButton.IsEnabled = false;
        BusyIndicator.IsRunning = true;
        BusyIndicator.IsVisible = true;

        try
        {
            string answer = await _session!.RunAsync(text);
            AppendBubble(answer, user: false);

            if (ProjectAccessService.IsLinked)
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

    private void AppendBubble(string text, bool user, bool isTool = false, bool isError = false)
    {
        Color background = user
            ? Color.FromArgb("#2c3a63")
            : isError
                ? Color.FromArgb("#4a2326")
                : isTool
                    ? Color.FromArgb("#22222b")
                    : Color.FromArgb("#1b1b22");
        LayoutOptions alignment = user ? LayoutOptions.End : LayoutOptions.Start;
        Color textColor = isTool
            ? Color.FromArgb("#9a9aa5")
            : Color.FromArgb("#f2f2f5");

        var label = new Label
        {
            Text = text,
            TextColor = textColor,
            FontSize = isTool ? 12 : 14,
            LineBreakMode = LineBreakMode.WordWrap
        };

        var border = new Border
        {
            BackgroundColor = background,
            StrokeThickness = 0,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 },
            Padding = new Thickness(10, 7),
            MaximumWidthRequest = 360,
            HorizontalOptions = alignment,
            Content = label
        };

        MainThread.BeginInvokeOnMainThread(() =>
        {
            ConversationContainer.Add(border);

            Dispatcher.Dispatch(() =>
            {
                ConversationScroll.ScrollToAsync(0, ConversationContainer.Height, true);
            });
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
