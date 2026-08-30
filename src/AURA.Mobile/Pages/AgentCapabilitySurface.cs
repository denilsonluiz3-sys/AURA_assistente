using System.Collections.Specialized;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using AURA.Abstractions.Execution;
using AURA.Modules.Executors;

namespace AURA.Mobile.Pages;

/// <summary>
/// Superfície visual temporária para capacidades executadas pelo agente.
/// Observa saída incremental dos executores e apresenta o estado da execução.
/// </summary>
public sealed class AgentCapabilitySurface : ContentView
{
    private readonly Border _card;
    private readonly Label _title;
    private readonly Label _status;
    private readonly Editor _output;
    private ProcessRegistry? _processes;
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
            Content = new VerticalStackLayout
            {
                Spacing = 4,
                Children = { _title, _status, _output }
            }
        };

        Content = _card;
        IsVisible = false;
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        if (Handler?.MauiContext?.Services is { } services)
            Bind(services);
    }

    public void Bind(IServiceProvider services)
    {
        if (_bound)
            return;

        _processes = services.GetService<ProcessRegistry>();
        if (_processes != null)
            _processes.Processes.CollectionChanged += OnProcessesChanged;

        ProcessExecutorBase.OutputReceived += OnProcessOutput;
        _bound = true;
        RefreshFromProcesses();
    }

    private void OnProcessesChanged(object? sender, NotifyCollectionChangedEventArgs e)
        => MainThread.BeginInvokeOnMainThread(RefreshFromProcesses);

    private void OnProcessOutput(object? sender, ProcessOutputEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (_activeWorkingDirectory == null)
                return;

            if (!SameDirectory(e.WorkingDirectory, _activeWorkingDirectory))
                return;

            Show(PathTitle(e.FileName), e.IsError ? "stderr" : "executando");
            AppendOutput(e.IsError ? "[stderr] " + e.Text : e.Text);
        });
    }

    private void RefreshFromProcesses()
    {
        if (_processes == null)
            return;

        var active = _processes.Processes
            .LastOrDefault(p => !IsTerminalStatus(p.Status));

        if (active != null)
        {
            var processId = active.Id?.ToString();
            if (_activeProcessId != processId)
            {
                _activeProcessId = processId;
                _activeWorkingDirectory = AgentWorkspaceRoot();
                _output.Text = string.Empty;
                Show(processId ?? "Processo", active.Status);
            }
            return;
        }

        var latest = _processes.Processes.LastOrDefault();
        if (latest != null)
        {
            _activeProcessId = latest.Id?.ToString();
            _activeWorkingDirectory = AgentWorkspaceRoot();
            Show(_activeProcessId ?? "Processo", latest.Status);
        }
    }

    /// <summary>
    /// Inicia uma apresentação visual para uma execução disparada diretamente
    /// pelo Agente. O conteúdo anterior é descartado para não misturar execuções.
    /// </summary>
    public void BeginExecution(string title, string workingDirectory)
    {
        _activeProcessId = null;
        _activeWorkingDirectory = string.IsNullOrWhiteSpace(workingDirectory)
            ? Directory.GetCurrentDirectory()
            : workingDirectory;
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
        if (string.IsNullOrEmpty(text))
            return;

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

    private static bool SameDirectory(string? left, string? right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
            return false;

        try
        {
            return string.Equals(
                System.IO.Path.GetFullPath(left),
                System.IO.Path.GetFullPath(right),
                StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static string PathTitle(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return "Execução";

        try
        {
            return System.IO.Path.GetFileName(path);
        }
        catch
        {
            return path;
        }
    }

    private static string AgentWorkspaceRoot()
    {
        // O AgentPage já estabelece o workspace como diretório de execução.
        // Usamos o cwd do processo como fallback sem criar uma segunda fonte de verdade.
        return Directory.GetCurrentDirectory();
    }

    protected override void OnParentSet()
    {
        base.OnParentSet();
        if (Parent == null && _bound)
        {
            if (_processes != null)
                _processes.Processes.CollectionChanged -= OnProcessesChanged;
            ProcessExecutorBase.OutputReceived -= OnProcessOutput;
            _bound = false;
            _activeProcessId = null;
            _activeWorkingDirectory = null;
        }
    }
}
