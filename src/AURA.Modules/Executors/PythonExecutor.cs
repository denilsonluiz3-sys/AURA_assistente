using AURA.Abstractions.Execution;

namespace AURA.Modules.Executors;

/// <summary>
/// Executor Python: 1) binário no PATH/Termux  2) interpretador embutido no APK.
/// </summary>
public sealed class PythonExecutor : ProcessExecutorBase
{
    /// <summary>Injetado pelo host Android (MauiProgram) quando o APK traz Python embutido.</summary>
    public static IEmbeddedPython? Embedded { get; set; }

    public override string Name => "python";

    public override bool IsAvailable() =>
        ResolveBinary("python3", "python") is not null || Embedded is not null;

    public override async Task<ExecutionResult> ExecuteAsync(
        ExecutionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (ResolveBinary("python3", "python") is { } binary)
        {
            var args = new List<string> { request.Command };
            args.AddRange(request.Arguments);
            return await RunAsync(binary, args, request, cancellationToken).ConfigureAwait(false);
        }

        if (Embedded is null)
            return ExecutionResult.Failed(
                "Python não encontrado (PATH/Termux) e interpretador embutido não está disponível.");

        try
        {
            await Embedded.EnsureReadyAsync(cancellationToken).ConfigureAwait(false);

            string cmd = request.Command?.Trim() ?? string.Empty;
            string output;

            // -c "código"  ou  script.py
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
                     || File.Exists(cmd))
            {
                string path = cmd;
                if (!Path.IsPathRooted(path) && !string.IsNullOrWhiteSpace(request.WorkingDirectory))
                    path = Path.Combine(request.WorkingDirectory, cmd);
                output = await Embedded.RunFileAsync(path, cancellationToken).ConfigureAwait(false);
            }
            else if (!string.IsNullOrWhiteSpace(cmd) && cmd.Contains('\n'))
            {
                // bloco multilinha como comando
                output = await Embedded.RunCodeAsync(cmd, cancellationToken).ConfigureAwait(false);
            }
            else if (!string.IsNullOrWhiteSpace(cmd))
            {
                // trata comando como código de uma linha
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
        catch (Exception ex)
        {
            return ExecutionResult.Failed("[Python embutido] " + ex.Message);
        }
    }
}
