using AURA.Modules.Executors;

namespace AURA.Mobile.Pages;

public partial class ExecutorsPage : ContentPage
{
    private readonly ShellExecutor _shell;
    private readonly GitExecutor _git;
    private readonly PythonExecutor _python;
    private readonly NodeExecutor _node;

    public ExecutorsPage(ShellExecutor shell, GitExecutor git, PythonExecutor python, NodeExecutor node)
    {
        InitializeComponent();
        _shell = shell;
        _git = git;
        _python = python;
        _node = node;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await RefreshAsync();
    }

    private async void OnRefreshClicked(object sender, EventArgs e)
    {
        await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        var statuses = new[]
        {
            MakeStatus(_shell),
            MakeStatus(_git),
            MakeStatus(_python),
            MakeStatus(_node)
        };

        ExecutorsView.ItemsSource = statuses;
        await Task.CompletedTask;
    }

    private static ExecutorStatus MakeStatus(ProcessExecutorBase executor)
    {
        bool available = executor.IsAvailable();
        return new ExecutorStatus
        {
            Name = executor.Name,
            Status = available ? "Disponível neste dispositivo" : "Não disponível no Android",
            StatusColor = available
                ? Color.FromArgb("#4caf6f")
                : Color.FromArgb("#e2555c")
        };
    }
}

public class ExecutorStatus
{
    public string Name { get; set; } = "";
    public string Status { get; set; } = "";
    public Color StatusColor { get; set; } = Colors.Gray;
}
