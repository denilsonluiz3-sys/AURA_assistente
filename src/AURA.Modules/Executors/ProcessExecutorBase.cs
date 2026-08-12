using System.Diagnostics;
using System.Text;
using AURA.Abstractions.Execution;

namespace AURA.Modules.Executors;

public abstract class ProcessExecutorBase : IToolExecutor
{
    public abstract string Name { get; }
    public abstract bool IsAvailable();
    public abstract Task<ExecutionResult> ExecuteAsync(ExecutionRequest request, CancellationToken cancellationToken = default);

    protected static async Task<ExecutionResult> RunAsync(
        string fileName,
        IEnumerable<string> arguments,
        ExecutionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await RunProcessAsync(fileName, arguments, request, cancellationToken);

        if (result.ExitCode == 127 && result.StandardError.Contains("shared library", StringComparison.OrdinalIgnoreCase))
        {
            var shellArgs = BuildShellCommand(fileName, arguments);
            var fallbackRequest = new ExecutionRequest
            {
                Command = request.Command,
                Arguments = request.Arguments,
                WorkingDirectory = request.WorkingDirectory ?? Directory.GetCurrentDirectory(),
                EnvironmentVariables = request.EnvironmentVariables,
                Timeout = request.Timeout
            };
            result = await RunProcessAsync(
                "/data/data/com.termux/files/usr/bin/bash",
                shellArgs,
                fallbackRequest,
                cancellationToken);
        }

        return result;
    }

    private static List<string> BuildShellCommand(string fileName, IEnumerable<string> arguments)
    {
        var escapedArgs = string.Join(" ", arguments.Select(a => a.Contains(' ') ? $"'{a}'" : a));
        return ["-c", $"{fileName} {escapedArgs}"];
    }

    private static async Task<ExecutionResult> RunProcessAsync(
        string fileName,
        IEnumerable<string> arguments,
        ExecutionRequest request,
        CancellationToken cancellationToken)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            WorkingDirectory = request.WorkingDirectory ?? Directory.GetCurrentDirectory(),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (var arg in arguments)
            psi.ArgumentList.Add(arg);

        if (request.EnvironmentVariables is not null)
        {
            foreach (var (key, value) in request.EnvironmentVariables)
                psi.Environment[key] = value;
        }

        using var process = new Process { StartInfo = psi };
        var stopwatch = Stopwatch.StartNew();

        try
        {
            process.Start();
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return ExecutionResult.Failed($"[AURA] Falha ao iniciar '{fileName}': {ex.Message}");
        }

        var tcs = new TaskCompletionSource<bool>();
        process.Exited += (_, _) => tcs.TrySetResult(true);
        process.EnableRaisingEvents = true;

        using var cts = request.Timeout is not null
            ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)
            : null;
        cts?.CancelAfter(request.Timeout!.Value);

        using var _ = cts?.Token.Register(() =>
        {
            try { process.Kill(entireProcessTree: true); } catch { }
            tcs.TrySetResult(false);
        });

        await tcs.Task;
        stopwatch.Stop();

        var stdout = await process.StandardOutput.ReadToEndAsync();
        var stderr = await process.StandardError.ReadToEndAsync();

        return new ExecutionResult
        {
            Success = process.ExitCode == 0,
            ExitCode = process.ExitCode,
            StandardOutput = stdout,
            StandardError = stderr,
            Duration = stopwatch.Elapsed
        };
    }

    /// <summary>Procura o primeiro binário disponível dentre os candidatos, olhando o PATH.</summary>
    protected static string? ResolveBinary(params string[] candidates)
    {
        var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        var dirs = pathEnv.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);

        foreach (var candidate in candidates)
        {
            foreach (var dir in dirs)
            {
                var fullPath = Path.Combine(dir, candidate);
                if (File.Exists(fullPath))
                    return fullPath;
            }
        }

        return null;
    }
}
