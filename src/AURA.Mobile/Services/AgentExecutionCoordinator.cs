using AURA.Abstractions.Execution;

namespace AURA.Mobile.Services;

/// <summary>
/// Orquestra uma execução de capacidade mantendo uma única identidade de processo.
/// A camada de UI pode vincular AgentCapabilitySurface ao CorrelationId devolvido
/// antes de iniciar o executor. O mesmo contrato serve para shell, git, python,
/// node e qualquer IToolExecutor existente.
/// </summary>
public sealed class AgentExecutionCoordinator
{
    private readonly ProcessRegistry _processes;

    public AgentExecutionCoordinator(ProcessRegistry processes)
    {
        _processes = processes;
    }

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

        string workingDirectory = string.IsNullOrWhiteSpace(request.WorkingDirectory)
            ? AgentWorkspace.ActiveRoot
            : request.WorkingDirectory;

        var process = _processes.Begin(title, executor.Name, "executando");
        request.WorkingDirectory = workingDirectory;
        request.CorrelationId = process.Id;

        _processes.Update(process.Id, title, "executando", 0.1);

        try
        {
            ExecutionResult result = await executor.ExecuteAsync(request, cancellationToken).ConfigureAwait(false);
            if (result.Success)
                _processes.Complete(process.Id, "concluído");
            else
                _processes.Fail(process.Id, result.StandardError.Trim().Length > 0
                    ? result.StandardError.Trim()
                    : $"exit={result.ExitCode}");

            return new AgentExecutionResult(process.Id, executor.Name, result);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _processes.Fail(process.Id, "cancelado");
            return AgentExecutionResult.Failed(process.Id, executor.Name, "Execução cancelada.");
        }
        catch (Exception ex)
        {
            _processes.Fail(process.Id, ex.Message);
            return AgentExecutionResult.Failed(process.Id, executor.Name, ex.Message);
        }
    }
}

public sealed record AgentExecutionResult(
    string? ProcessId,
    string Executor,
    ExecutionResult Result)
{
    public bool Success => Result.Success;

    public static AgentExecutionResult Failed(string message)
        => new(null, "none", ExecutionResult.Failed(message));

    public static AgentExecutionResult Failed(string processId, string executor, string message)
        => new(processId, executor, ExecutionResult.Failed(message));
}
