using AURA.Abstractions.Execution;

namespace AURA.Modules.Executors;

/// <summary>
/// Shell multi-candidato (Android toybox/sh, Termux, /bin/sh).
/// IsAvailable tenta vários caminhos — Xiaomi/SELinux às vezes escondem /system/bin/sh.
/// </summary>
public sealed class ShellExecutor : ProcessExecutorBase
{
    public override string Name => "shell";

    private sealed record ShellBin(string FileName, string[] PrefixArgs);

    private static readonly ShellBin[] Candidates =
    {
        new("/system/bin/sh", Array.Empty<string>()),
        new("/system/bin/toybox", new[] { "sh" }),
        new("/vendor/bin/sh", Array.Empty<string>()),
        new("/bin/sh", Array.Empty<string>()),
        new("/system/xbin/bash", Array.Empty<string>()),
        new("/data/data/com.termux/files/usr/bin/bash", Array.Empty<string>()),
        new("/data/data/com.termux/files/usr/bin/sh", Array.Empty<string>()),
    };

    private static ShellBin? _resolved;

    private static ShellBin? Resolve()
    {
        if (_resolved != null)
            return _resolved;

        foreach (var c in Candidates)
        {
            try
            {
                if (File.Exists(c.FileName))
                {
                    _resolved = c;
                    return c;
                }
            }
            catch { /* ignore probe errors */ }
        }

        // Última tentativa: "sh" no PATH
        try
        {
            var fromPath = ResolveBinary("sh", "bash", "toybox");
            if (fromPath != null)
            {
                _resolved = fromPath.EndsWith("toybox", StringComparison.OrdinalIgnoreCase)
                    ? new ShellBin(fromPath, new[] { "sh" })
                    : new ShellBin(fromPath, Array.Empty<string>());
                return _resolved;
            }
        }
        catch { /* ignore */ }

        return null;
    }

    public override bool IsAvailable() => Resolve() != null;

    public static string DescribeAvailability()
    {
        var s = Resolve();
        if (s == null)
        {
            var tried = string.Join(", ", Candidates.Select(c => c.FileName));
            return "Shell indisponível. Tentados: " + tried +
                   ". Alternativas: instalar Termux; conceder armazenamento; ADB shell só via PC (adb shell).";
        }
        return "Shell OK: " + s.FileName +
               (s.PrefixArgs.Length > 0 ? " " + string.Join(' ', s.PrefixArgs) : "");
    }

    public override Task<ExecutionResult> ExecuteAsync(
        ExecutionRequest request,
        CancellationToken cancellationToken = default)
    {
        var shell = Resolve();
        if (shell == null)
            return Task.FromResult(ExecutionResult.Failed(DescribeAvailability()));

        if (string.IsNullOrWhiteSpace(request.Command))
            return Task.FromResult(ExecutionResult.Failed("Comando Shell vazio."));

        var fullCommand = request.Arguments.Count > 0
            ? $"{request.Command} {string.Join(' ', request.Arguments)}"
            : request.Command;

        var args = new List<string>(shell.PrefixArgs.Length + 2);
        args.AddRange(shell.PrefixArgs);
        args.Add("-c");
        args.Add(fullCommand);

        return RunAsync(shell.FileName, args, request, cancellationToken);
    }
}
