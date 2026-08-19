using System.Collections.ObjectModel;
using AURA.Core.Events;

namespace AURA.Mobile;

public sealed class ProcessRegistry
{
    private readonly object _sync = new();
    private readonly Dictionary<string, ProcessInfo> _byId = new(StringComparer.OrdinalIgnoreCase);
    private readonly EventBus _events;

    public ObservableCollection<ProcessInfo> Processes { get; } = new();

    public ProcessRegistry(EventBus events)
    {
        _events = events;
        _events.Subscribe<CellStateChangedEvent>(OnCellStateChanged);
        _events.Subscribe<OrchestrationStepEvent>(OnOrchestrationStep);
    }

    public ProcessInfo Begin(string title, string target, string? message = null)
    {
        var process = new ProcessInfo
        {
            Id = Guid.NewGuid().ToString("N"),
            Title = title,
            Target = target,
            Message = message ?? "Executando",
            Status = "Executando",
            Progress = 0
        };

        lock (_sync)
        {
            _byId[process.Id] = process;
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Processes.Insert(0, process);
                Trim();
            });
        }

        return process;
    }

    public void Complete(string id, string message = "Concluído") => Update(id, "Concluído", message, 1);

    public void Fail(string id, string message = "Falhou") => Update(id, "Falhou", message, 1);

    public void Retry(string id, string message = "Tentando novamente") => Update(id, "Tentando novamente", message, 0);

    public void Update(string id, string status, string message, double progress)
    {
        ProcessInfo? process;
        lock (_sync)
            _byId.TryGetValue(id, out process);

        if (process == null)
            return;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            process.Status = status;
            process.Message = message;
            process.Progress = Math.Clamp(progress, 0, 1);
        });
    }

    private void OnOrchestrationStep(OrchestrationStepEvent evt)
    {
        if (string.IsNullOrWhiteSpace(evt.Id))
            return;

        ProcessInfo? process;
        lock (_sync)
            _byId.TryGetValue(evt.Id, out process);

        if (process == null)
        {
            process = new ProcessInfo
            {
                Id = evt.Id,
                Title = evt.Title,
                Target = evt.Target,
                Status = evt.Status,
                Message = evt.Message,
                Progress = Math.Clamp(evt.Progress, 0, 1),
                StartedAt = evt.OccurredAt
            };

            lock (_sync)
                _byId[process.Id] = process;

            var created = process;
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Processes.Insert(0, created);
                Trim();
            });
            return;
        }

        MainThread.BeginInvokeOnMainThread(() =>
        {
            process.Title = evt.Title;
            process.Target = evt.Target;
            process.Status = evt.Status;
            process.Message = evt.Message;
            process.Progress = Math.Clamp(evt.Progress, 0, 1);
        });
    }

    private void OnCellStateChanged(CellStateChangedEvent evt)
    {
        if (string.IsNullOrWhiteSpace(evt.CellId))
            return;

        string state = evt.To ?? string.Empty;
        string status = state switch
        {
            _ when state.Contains("fail", StringComparison.OrdinalIgnoreCase) ||
                   state.Contains("error", StringComparison.OrdinalIgnoreCase) => "Falhou",
            _ when state.Contains("stop", StringComparison.OrdinalIgnoreCase) ||
                   state.Contains("complete", StringComparison.OrdinalIgnoreCase) => "Concluído",
            _ when state.Contains("pause", StringComparison.OrdinalIgnoreCase) => "Pausado",
            _ when state.Contains("start", StringComparison.OrdinalIgnoreCase) ||
                   state.Contains("run", StringComparison.OrdinalIgnoreCase) => "Executando",
            _ => state
        };

        string processId = "cell:" + evt.CellId;
        ProcessInfo? process;
        lock (_sync)
            _byId.TryGetValue(processId, out process);

        if (process == null)
        {
            process = new ProcessInfo
            {
                Id = processId,
                CellId = evt.CellId,
                Title = "Célula " + evt.CellId,
                Target = "Cells",
                Status = status,
                Message = state,
                Progress = status == "Concluído" || status == "Falhou" ? 1 : 0,
                StartedAt = evt.OccurredAt
            };

            lock (_sync)
                _byId[process.Id] = process;

            var created = process;
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Processes.Insert(0, created);
                Trim();
            });
            return;
        }

        Update(process.Id, status, state, status == "Concluído" || status == "Falhou" ? 1 : process.Progress);
    }

    private void Trim()
    {
        while (Processes.Count > 8)
        {
            var last = Processes[^1];
            Processes.RemoveAt(Processes.Count - 1);
            lock (_sync)
                _byId.Remove(last.Id);
        }
    }
}
