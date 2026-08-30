using AURA.Abstractions.Execution;
using AURA.Modules.Executors;

namespace AURA.Mobile.Services;

/// <summary>Porta única para execuções iniciadas pelo Agente.</summary>
public sealed class AgentExecutionCoordinator : IDisposable
{
    private readonly ProcessRegistry _processes;
    private bool _disposed;

    public AgentExecutionCoordinator(ProcessRegistry processes)
    {
        _processes = processes;
        ProcessExecutorBase.OutputReceived += OnProcessOutput;
    }

    public event EventHandler<AgentExecutionStartedEventArgs>? Started;
    public event EventHandler<AgentExecutionOutputEventArgs>? Output;
    public event EventHandler<AgentExecutionCompletedEventArgs>? Completed;

    public async Task<AgentExecutionResult> ExecuteAsync(IToolExecutor executor, string title, ExecutionRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(executor);
        ArgumentNullException.ThrowIfNull(request);
        if (!executor.IsAvailable()) return AgentExecutionResult.Failed($"Executor indisponível: {executor.Name}");

        request.WorkingDirectory = string.IsNullOrWhiteSpace(request.WorkingDirectory) ? Directory.GetCurrentDirectory() : request.WorkingDirectory;
        var process = _processes.Begin(title, executor.Name, "executando");
        request.CorrelationId = process.Id;
        _processes.Update(process.Id, title, "executando", 0.1);
        Started?.Invoke(this, new AgentExecutionStartedEventArgs(process.Id, title, executor.Name, request.WorkingDirectory));

        try
        {
            var result = await executor.ExecuteAsync(request, cancellationToken).ConfigureAwait(false);
            if (result.Success) _processes.Complete(process.Id, "concluído");
            else _processes.Fail(process.Id, string.IsNullOrWhiteSpace(result.StandardError) ? $"exit={result.ExitCode}" : result.StandardError.Trim());
            Completed?.Invoke(this, new AgentExecutionCompletedEventArgs(process.Id, executor.Name, result));
            return new AgentExecutionResult(process.Id, executor.Name, result);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _processes.Fail(process.Id, "cancelado");
            var result = ExecutionResult.Failed("Execução cancelada.");
            Completed?.Invoke(this, new AgentExecutionCompletedEventArgs(process.Id, executor.Name, result));
            return new AgentExecutionResult(process.Id, executor.Name, result);
        }
        catch (Exception ex)
        {
            _processes.Fail(process.Id, ex.Message);
            var result = ExecutionResult.Failed(ex.Message);
            Completed?.Invoke(this, new AgentExecutionCompletedEventArgs(process.Id, executor.Name, result));
            return new AgentExecutionResult(process.Id, executor.Name, result);
        }
    }

    private void OnProcessOutput(object? sender, ProcessOutputEventArgs e)
    {
        if (_disposed || string.IsNullOrWhiteSpace(e.CorrelationId) || string.IsNullOrEmpty(e.Text)) return;
        Output?.Invoke(this, new AgentExecutionOutputEventArgs(e.CorrelationId, e.IsError ? "stderr" : "stdout", e.Text));
    }

    public void PublishOutput(string correlationId, string stream, string text)
    {
        if (_disposed || string.IsNullOrWhiteSpace(correlationId) || string.IsNullOrEmpty(text)) return;
        Output?.Invoke(this, new AgentExecutionOutputEventArgs(correlationId, stream, text));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        ProcessExecutorBase.OutputReceived -= OnProcessOutput;
    }
}

public sealed class AgentExecutionStartedEventArgs : EventArgs
{
    public AgentExecutionStartedEventArgs(string processId, string title, string executor, string workingDirectory) { ProcessId = processId; Title = title; Executor = executor; WorkingDirectory = workingDirectory; }
    public string ProcessId { get; }
    public string Title { get; }
    public string Executor { get; }
    public string WorkingDirectory { get; }
}

public sealed class AgentExecutionOutputEventArgs : EventArgs
{
    public AgentExecutionOutputEventArgs(string correlationId, string stream, string text) { CorrelationId = correlationId; Stream = stream; Text = text; }
    public string CorrelationId { get; }
    public string Stream { get; }
    public string Text { get; }
}

public sealed class AgentExecutionCompletedEventArgs : EventArgs
{
    public AgentExecutionCompletedEventArgs(string processId, string executor, ExecutionResult result) { ProcessId = processId; Executor = executor; Result = result; }
    public string ProcessId { get; }
    public string Executor { get; }
    public ExecutionResult Result { get; }
}

public sealed record AgentExecutionResult(string? ProcessId, string Executor, ExecutionResult Result)
{
    public bool Success => Result.Success;
    public static AgentExecutionResult Failed(string message) => new(null, "none", ExecutionResult.Failed(message));
}
