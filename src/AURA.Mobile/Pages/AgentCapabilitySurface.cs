using System.Collections.Specialized;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using AURA.Abstractions.Execution;
using AURA.Modules.Executors;

namespace AURA.Mobile.Pages;

/// <summary>
/// Superfície visual temporária para capacidades executadas pelo agente.
/// Observa o registry de processos e apresenta saída incremental.
/// </summary>
public sealed class AgentCapabilitySurface : ContentView
{
    private readonly Border _card;
    private readonly Label _title;
    private readonly Label _status;
    private readonly Editor _output;
    private ProcessRegistry? _processes;
    private Guid? _activeProcessId;
    private bool _bound;

    public AgentCapabilitySurface()
    {
        _title = new Label
        {
            FontSize = 13,
            FontAttributes = FontAttributes.Bold
        };

        _status = new Label
        {
            FontSize = 11,
            Opacity = 0.75
        };

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
            if (_activeProcessId == null)
                Show(System.IO.Path.GetFileName(e.FileName), e.IsError ? "stderr" : "executando");

            AppendOutput(e.Text);
        });
    }

    private void RefreshFromProcesses()
    {
        if (_processes == null)
            return;

        var active = _processes.Processes
            .LastOrDefault(p => !string.Equals(p.Status, "Concluído", StringComparison.OrdinalIgnoreCase)
                              && !string.Equals(p.Status, "Falhou", StringComparison.OrdinalIgnoreCase));

        if (active != null)
        {
            if (_activeProcessId != active.Id)
            {
                _activeProcessId = active.Id;
                Show(active.Command, active.Status);
            }
            return;
        }

        var latest = _processes.Processes.LastOrDefault();
        if (latest != null)
        {
            _activeProcessId = latest.Id;
            Show(latest.Command, latest.Status);
        }
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
            IsVisible = false;
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
        }
    }
}
