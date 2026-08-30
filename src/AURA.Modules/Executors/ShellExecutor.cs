using AURA.Abstractions.Execution;

namespace AURA.Modules.Executors;

/// <summary>
/// Executor de shell da AURA. No Android usa o shell do próprio sistema;
/// em outros ambientes usa /bin/sh quando disponível.
/// </summary>
public sealed class ShellExecutor : ProcessExecutorBase
{
    public override string Name => "shell";

    private static string ShellPath =>
        OperatingSystem.IsAndroid() && File.Exists("/system/bin/sh")
            ? "/system/bin/sh"
            : "/bin/sh";

    public override bool IsAvailable() => File.Exists(ShellPath);

    public override Task<ExecutionResult> ExecuteAsync(
        ExecutionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!IsAvailable())
            return Task.FromResult(
                ExecutionResult.Failed($"Shell não encontrado: {ShellPath}"));

        if (string.IsNullOrWhiteSpace(request.Command))
            return Task.FromResult(
                ExecutionResult.Failed("Comando Shell vazio."));

        var fullCommand = request.Arguments.Count > 0
            ? $"{request.Command} {string.Join(' ', request.Arguments)}"
            : request.Command;

        return RunAsync(
            ShellPath,
            new[] { "-c", fullCommand },
            request,
            cancellationToken);
    }
}
