using AURA.Abstractions.Execution;

namespace AURA.Mobile.Services;

/// <summary>
/// Porta única para execuções iniciadas pelo Agente. A execução recebe uma
/// identidade no ProcessRegistry antes de chegar ao executor; a mesma identidade
/// acompanha a saída incremental e pode ser ligada à AgentCapabilitySurface.
/// </summary>
public sealed class AgentExecutionCoordinator
{
    private readonly ProcessRegistry _processes;

    public AgentExecutionCoordinator(ProcessRegistry processes)
    {
        _processes = processes;
    }

    public event EventHandler<AgentExecutionStartedEventArgs>? Started;
    public event EventHandler<AgentExecutionOutputEventArgs>? Output;
    public event EventHandler<AgentExecutionCompletedEventArgs>? Completed;

    public async Task<AgentExecutionResult> ExecuteAsync(
        IToolExecutor executor,
        string title,
        ExecutionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(executor);
        ArgumentNullException.ThrowIfNull(request);

        if (!executor.IsAvailable())
            return AgentExecutionResult.Failed($"Executor indisponível: {executor.Name}");

        request.WorkingDirectory = string.IsNullOrWhiteSpace(request.WorkingDirectory)
            ? Directory.GetCurrentDirectory()
            : request.WorkingDirectory;

        var process = _processes.Begin(title, executor.Name, "executando");
        request.CorrelationId = process.Id;
        _processes.Update(process.Id, title, "executando", 0.1);
        Started?.Invoke(this, new AgentExecutionStartedEventArgs(process.Id, title, executor.Name, request.WorkingDirectory));

        try
        {
            ExecutionResult result = await executor.ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

            if (result.Success)
                _processes.Complete(process.Id, "concluído");
            else
                _processes.Fail(process.Id, string.IsNullOrWhiteSpace(result.StandardError)
                    ? $"exit={result.ExitCode}"
                    : result.StandardError.Trim());

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

    /// <summary>Encaminha saída incremental já associada a uma execução.</summary>
    public void PublishOutput(string correlationId, string stream, string text)
    {
        if (string.IsNullOrWhiteSpace(correlationId) || string.IsNullOrEmpty(text))
            return;

        Output?.Invoke(this, new AgentExecutionOutputEventArgs(correlationId, stream, text));
    }
}

public sealed record AgentExecutionStartedEventArgs(
    string ProcessId,
    string Title,
    string Executor,
    string WorkingDirectory) : EventArgs;

public sealed record AgentExecutionOutputEventArgs(
    string CorrelationId,
    string Stream,
    string Text) : EventArgs;

public sealed record AgentExecutionCompletedEventArgs(
    string ProcessId,
    string Executor,
    ExecutionResult Result) : EventArgs;

public sealed record AgentExecutionResult(
    string? ProcessId,
    string Executor,
    ExecutionResult Result)
{
    public bool Success => Result.Success;

    public static AgentExecutionResult Failed(string message)
        => new(null, "none", ExecutionResult.Failed(message));
}
