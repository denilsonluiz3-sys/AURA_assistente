using System.Diagnostics;
using System.Text;
using AURA.Abstractions.Execution;

namespace AURA.Modules.Executors;

public sealed class ProcessOutputEventArgs : EventArgs
{
    public ProcessOutputEventArgs(string fileName, string workingDirectory, bool isError, string text)
    {
        FileName = fileName;
        WorkingDirectory = workingDirectory;
        IsError = isError;
        Text = text;
    }

    public string FileName { get; }
    public string WorkingDirectory { get; }
    public bool IsError { get; }
    public string Text { get; }
}

public abstract class ProcessExecutorBase : IToolExecutor
{
    /// <summary>
    /// Saída incremental de processos. A UI pode observar este evento sem
    /// criar outro executor ou alterar o contrato de IToolExecutor.
    /// </summary>
    public static event EventHandler<ProcessOutputEventArgs>? OutputReceived;

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

    private static void ApplyTermuxEnvironment(ProcessStartInfo psi, string fileName)
    {
        const string termuxRoot = "/data/data/com.termux/files/usr";
        if (!OperatingSystem.IsAndroid() || !fileName.StartsWith(termuxRoot, StringComparison.Ordinal))
            return;

        var binDir = Path.Combine(termuxRoot, "bin");
        var libDir = Path.Combine(termuxRoot, "lib");
        var home = Path.Combine("/data/data/com.termux/files/home");

        if (!psi.Environment.ContainsKey("PATH"))
        {
            var currentPath = psi.Environment.TryGetValue("PATH", out var existingPath)
                ? existingPath
                : Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            psi.Environment["PATH"] = binDir + Path.PathSeparator + currentPath;
        }
        else
        {
            psi.Environment["PATH"] = binDir + Path.PathSeparator + psi.Environment["PATH"];
        }

        if (Directory.Exists(libDir))
            psi.Environment["LD_LIBRARY_PATH"] = libDir;

        if (!psi.Environment.ContainsKey("HOME") && Directory.Exists(home))
            psi.Environment["HOME"] = home;
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
        var workingDirectory = request.WorkingDirectory ?? Directory.GetCurrentDirectory();
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        ApplyTermuxEnvironment(psi, fileName);

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

        var stdout = new StringBuilder();
        var stderr = new StringBuilder();

        Task ReadStreamAsync(StreamReader reader, StringBuilder buffer, bool isError) =>
            Task.Run(async () =>
            {
                while (true)
                {
                    string? line;
                    try
                    {
                        line = await reader.ReadLineAsync().ConfigureAwait(false);
                    }
                    catch
                    {
                        break;
                    }

                    if (line is null)
                        break;

                    buffer.AppendLine(line);
                    OutputReceived?.Invoke(null, new ProcessOutputEventArgs(
                        fileName,
                        workingDirectory,
                        isError,
                        line + Environment.NewLine));
                }
            });

        var stdoutTask = ReadStreamAsync(process.StandardOutput, stdout, false);
        var stderrTask = ReadStreamAsync(process.StandardError, stderr, true);

        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var timedOut = false;
        process.Exited += (_, _) => tcs.TrySetResult(true);
        process.EnableRaisingEvents = true;

        using var cts = request.Timeout is not null
            ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)
            : null;
        cts?.CancelAfter(request.Timeout!.Value);

        using var _ = cts?.Token.Register(() =>
        {
            timedOut = true;
            try { process.Kill(entireProcessTree: true); } catch { }
            tcs.TrySetResult(false);
        });

        await tcs.Task.ConfigureAwait(false);
        await Task.WhenAll(stdoutTask, stderrTask).ConfigureAwait(false);
        stopwatch.Stop();

        if (timedOut)
            stderr.Append("[AURA] Execução cancelada por timeout.\n");

        return new ExecutionResult
        {
            Success = !timedOut && process.ExitCode == 0,
            ExitCode = timedOut ? -1 : process.ExitCode,
            StandardOutput = stdout.ToString(),
            StandardError = stderr.ToString(),
            Duration = stopwatch.Elapsed
        };
    }

    protected static string? ResolveBinary(params string[] candidates)
    {
        var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        var dirs = pathEnv.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries).ToList();

        if (OperatingSystem.IsAndroid())
        {
            string[] termuxDirs =
            {
                "/data/data/com.termux/files/usr/bin",
                "/data/data/com.termux/files/usr/local/bin",
                "/data/data/com.termux/files/home/.local/bin"
            };
            foreach (var termuxDir in termuxDirs)
            {
                if (!dirs.Contains(termuxDir))
                    dirs.Add(termuxDir);
            }
        }

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
