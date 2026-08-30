using System.Collections.Specialized;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using AURA.Modules.Executors;

namespace AURA.Mobile.Pages;

/// <summary>
/// Superfície visual temporária para uma execução do Agente.
/// A correlação por ProcessId é a autoridade; cwd somente serve para compatibilidade.
/// </summary>
public sealed class AgentCapabilitySurface : ContentView
{
    private readonly Border _card;
    private readonly Label _title;
    private readonly Label _status;
    private readonly Editor _output;
    private ProcessRegistry? _processes;
    private AgentExecutionCoordinator? _coordinator;
    private string? _activeProcessId;
    private string? _activeWorkingDirectory;
    private bool _bound;

    public AgentCapabilitySurface()
    {
        _title = new Label { FontSize = 13, FontAttributes = FontAttributes.Bold };
        _status = new Label { FontSize = 11, Opacity = 0.75 };
        _output = new Editor
        {
            IsReadOnly = true,
            AutoSize = EditorAutoSizeOption.TextChanges,
            MinimumHeightRequest = 48,
            MaximumHeightRequest = 240,
            FontSize = 12,
            Text = string.Empty
        };
        _card = new Border
        {
            StrokeThickness = 0,
            Padding = new Thickness(12, 8),
            Margin = new Thickness(0, 4),
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(12) },
            Content = new VerticalStackLayout { Spacing = 4, Children = { _title, _status, _output } }
        };
        Content = _card;
        IsVisible = false;
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        if (Handler?.MauiContext?.Services is { } services) Bind(services);
    }

    public void Bind(IServiceProvider services)
    {
        if (_bound) return;
        _processes = services.GetService<ProcessRegistry>();
        _coordinator = services.GetService<AgentExecutionCoordinator>();
        if (_processes != null) _processes.Processes.CollectionChanged += OnProcessesChanged;
        if (_coordinator != null)
        {
            _coordinator.Started += OnExecutionStarted;
            _coordinator.Output += OnExecutionOutput;
            _coordinator.Completed += OnExecutionCompleted;
        }
        _bound = true;
        RefreshFromProcesses();
    }

    public void BindProcess(string processId, string? workingDirectory = null, string? title = null)
    {
        if (string.IsNullOrWhiteSpace(processId)) return;
        _activeProcessId = processId;
        _activeWorkingDirectory = workingDirectory;
        _output.Text = string.Empty;
        Show(title ?? processId, "executando");
    }

    private void OnExecutionStarted(object? sender, AgentExecutionStartedEventArgs e)
    {
        if (_activeProcessId != null) return;
        MainThread.BeginInvokeOnMainThread(() => BindProcess(e.ProcessId, e.WorkingDirectory, e.Title));
    }

    private void OnExecutionOutput(object? sender, AgentExecutionOutputEventArgs e)
    {
        if (!string.Equals(e.CorrelationId, _activeProcessId, StringComparison.OrdinalIgnoreCase)) return;
        MainThread.BeginInvokeOnMainThread(() =>
        {
            Show(_title.Text ?? "Execução", e.Stream == "stderr" ? "stderr" : "executando");
            AppendOutput(e.Stream == "stderr" ? "[stderr] " + e.Text : e.Text);
        });
    }

    private void OnExecutionCompleted(object? sender, AgentExecutionCompletedEventArgs e)
    {
        if (!string.Equals(e.ProcessId, _activeProcessId, StringComparison.OrdinalIgnoreCase)) return;
        MainThread.BeginInvokeOnMainThread(() =>
        {
            _status.Text = e.Result.Success ? "concluído" : "falhou";
        });
    }

    private void OnProcessesChanged(object? sender, NotifyCollectionChangedEventArgs e)
        => MainThread.BeginInvokeOnMainThread(RefreshFromProcesses);

    private void RefreshFromProcesses()
    {
        if (_processes == null || _activeProcessId != null) return;
        var active = _processes.Processes.LastOrDefault(p => !IsTerminalStatus(p.Status));
        if (active != null) BindProcess(active.Id, active.Title, active.Title);
    }

    public void BeginExecution(string title, string workingDirectory)
    {
        _activeProcessId = null;
        _activeWorkingDirectory = string.IsNullOrWhiteSpace(workingDirectory) ? Directory.GetCurrentDirectory() : workingDirectory;
        _output.Text = string.Empty;
        Show(title, "executando");
    }

    public void Show(string title, string status = "executando")
    {
        _title.Text = title;
        _status.Text = status;
        IsVisible = true;
    }

    public void AppendOutput(string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        _output.Text += text;
        _output.CursorPosition = _output.Text.Length;
    }

    public void Complete(bool success, bool hide = true)
    {
        _status.Text = success ? "concluído" : "falhou";
        if (hide)
        {
            IsVisible = false;
            _activeProcessId = null;
            _activeWorkingDirectory = null;
        }
    }

    private static bool IsTerminalStatus(string? status)
        => string.Equals(status, "Concluído", StringComparison.OrdinalIgnoreCase)
        || string.Equals(status, "Falhou", StringComparison.OrdinalIgnoreCase);

    protected override void OnParentSet()
    {
        base.OnParentSet();
        if (Parent == null && _bound)
        {
            if (_processes != null) _processes.Processes.CollectionChanged -= OnProcessesChanged;
            if (_coordinator != null)
            {
                _coordinator.Started -= OnExecutionStarted;
                _coordinator.Output -= OnExecutionOutput;
                _coordinator.Completed -= OnExecutionCompleted;
            }
            _bound = false;
            _activeProcessId = null;
            _activeWorkingDirectory = null;
        }
    }
}
