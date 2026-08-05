using AURA.AI;
using AURA.Mobile.Diagnostics;

namespace AURA.Mobile.Pages;

public partial class AgentPage : ContentPage
{
    private readonly OpenRouterClient _client;
    private AgentSession? _session;

    public AgentPage(OpenRouterClient client)
    {
        InitializeComponent();
        _client = client;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        RuntimeConfig.Apply(_client);

        string workspace = AgentWorkspace.EnsureCreated();
        WorkspaceLabel.Text = "Workspace: " + workspace +
            $" ({AgentWorkspace.CountFiles()} arquivo(s))";
        ModelLabel.Text = $"Modelo: {_client.Options.Model} · {_client.Options.BaseUrl}";

        EnsureSession();
    }

    private void EnsureSession()
    {
        if (_session != null)
        {
            return;
        }

        string root = AgentWorkspace.WorkspaceRoot;
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
            "dentro de um workspace no dispositivo (semelhante ao opencode). " +
            "Você PODE listar, ler, criar, editar e sobrescrever arquivos do " +
            "workspace e executar comandos shell (sh -c) nesse diretório. " +
            "Prefira ferramentas a respostas vagas: quando o usuário pedir uma " +
            "tarefa, use as ferramentas e confirme o que foi feito. " +
            "Responda em português, de forma curta e objetiva. " +
            "Caminhos são sempre relativos ao workspace.";

        _session = new AgentSession(_client, tools, systemPrompt);
        _session.Step += OnAgentStep;

        AppendBubble(
            "Pronto. Posso listar, ler, criar e editar arquivos do workspace e " +
            "rodar comandos shell. O que deseja fazer?", user: false);
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
        HorizontalOptions alignment = user ? LayoutOptions.End : LayoutOptions.Start;
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

        ConversationContainer.Add(border);

        Dispatcher.Dispatch(() =>
        {
            ConversationScroll.ScrollToAsync(0, ConversationContainer.Height, true);
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
