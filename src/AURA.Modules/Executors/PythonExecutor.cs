using AURA.Abstractions.Execution;

namespace AURA.Modules.Executors;

/// <summary>
/// Executor Python. No Android, prioriza o runtime embutido no APK;
/// em outros ambientes usa python3/python do PATH.
/// </summary>
public sealed class PythonExecutor : ProcessExecutorBase
{
    /// <summary>Injetado pelo host Android quando o APK traz Python embutido.</summary>
    public static IEmbeddedPython? Embedded { get; set; }

    public override string Name => "python";

    public override bool IsAvailable() =>
        OperatingSystem.IsAndroid()
            ? Embedded is not null
            : ResolveBinary("python3", "python") is not null || Embedded is not null;

    public override async Task<ExecutionResult> ExecuteAsync(
        ExecutionRequest request,
        CancellationToken cancellationToken = default)
    {
        // Android não deve depender do PATH do sistema ou do Termux.
        if (OperatingSystem.IsAndroid() && Embedded is not null)
            return await ExecuteEmbeddedAsync(request, cancellationToken).ConfigureAwait(false);

        if (ResolveBinary("python3", "python") is { } binary)
        {
            var args = new List<string> { request.Command };
            args.AddRange(request.Arguments);
            return await RunAsync(binary, args, request, cancellationToken).ConfigureAwait(false);
        }

        if (Embedded is not null)
            return await ExecuteEmbeddedAsync(request, cancellationToken).ConfigureAwait(false);

        return ExecutionResult.Failed(
            "Python não encontrado no PATH e o interpretador embutido não está disponível.");
    }

    private static async Task<ExecutionResult> ExecuteEmbeddedAsync(
        ExecutionRequest request,
        CancellationToken cancellationToken)
    {
        if (Embedded is null)
            return ExecutionResult.Failed("Interpretador Python embutido indisponível.");

        try
        {
            await Embedded.EnsureReadyAsync(cancellationToken).ConfigureAwait(false);

            string cmd = request.Command?.Trim() ?? string.Empty;
            string output;

            // -c "código"
            if (cmd is "-c" or "-c.py")
            {
                string code = request.Arguments.Count > 0
                    ? string.Join(" ", request.Arguments)
                    : string.Empty;
                if (string.IsNullOrWhiteSpace(code))
                    return ExecutionResult.Failed("python -c requer o código nos argumentos.");

                output = await Embedded.RunCodeAsync(code, cancellationToken).ConfigureAwait(false);
            }
            else if (cmd.EndsWith(".py", StringComparison.OrdinalIgnoreCase)
                     || File.Exists(ResolveScriptPath(cmd, request.WorkingDirectory)))
            {
                string path = ResolveScriptPath(cmd, request.WorkingDirectory);
                output = await Embedded.RunFileAsync(path, cancellationToken).ConfigureAwait(false);
            }
            else if (!string.IsNullOrWhiteSpace(cmd))
            {
                output = await Embedded.RunCodeAsync(cmd, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                return ExecutionResult.Failed("Comando Python vazio.");
            }

            return new ExecutionResult
            {
                Success = true,
                ExitCode = 0,
                StandardOutput = output ?? string.Empty,
                StandardError = string.Empty,
                Duration = TimeSpan.Zero
            };
        }
        catch (OperationCanceledException)
        {
            return ExecutionResult.Failed("Execução Python cancelada.");
        }
        catch (Exception ex)
        {
            return ExecutionResult.Failed("[Python embutido] " + ex.Message);
        }
    }

    private static string ResolveScriptPath(string path, string? workingDirectory)
    {
        if (Path.IsPathRooted(path) || string.IsNullOrWhiteSpace(workingDirectory))
            return path;

        return Path.Combine(workingDirectory, path);
    }
}
