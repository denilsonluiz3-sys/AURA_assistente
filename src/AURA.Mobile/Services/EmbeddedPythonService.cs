using AURA.Abstractions.Execution;
using AURA.Mobile.Diagnostics;

namespace AURA.Mobile.Services;

/// <summary>
/// Interpretador Python embutido via NuGet Python3Android.
/// Não depende do Termux nem do PATH do processo Android.
/// </summary>
public sealed class EmbeddedPythonService : IEmbeddedPython
{
    private readonly SemaphoreSlim _initGate = new(1, 1);
    private readonly SemaphoreSlim _runGate = new(1, 1);
    private object? _env;
    private bool _failed;

    public bool IsReady => _env is not null && !_failed;

    public async Task EnsureReadyAsync(CancellationToken ct = default)
    {
        if (_env is not null || _failed)
            return;

        await _initGate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (_env is not null || _failed)
                return;

#if ANDROID
            try
            {
                var t = Type.GetType("Python3Android.AndroidPythonEnvironment, Python3Android")
                    ?? Type.GetType("Python3Android.AndroidPythonEnvironment, python3android");

                if (t is null)
                {
                    foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        t = asm.GetType("Python3Android.AndroidPythonEnvironment");
                        if (t is not null) break;
                    }
                }

                if (t is null)
                {
                    AuraLog.Warning("EmbeddedPython: AndroidPythonEnvironment não encontrado.");
                    _failed = true;
                    return;
                }

                var create = t.GetMethod("Create", Type.EmptyTypes)
                    ?? t.GetMethods().FirstOrDefault(m => m.Name == "Create" && m.IsStatic);

                if (create is null)
                {
                    AuraLog.Warning("EmbeddedPython: método Create() não encontrado.");
                    _failed = true;
                    return;
                }

                object? result = create.Invoke(null, null);
                if (result is Task task)
                {
                    await task.ConfigureAwait(false);
                    _env = task.GetType().GetProperty("Result")?.GetValue(task);
                }
                else
                {
                    _env = result;
                }

                if (_env is null)
                {
                    _failed = true;
                    AuraLog.Warning("EmbeddedPython: Create() retornou null.");
                    return;
                }

                AuraLog.Info("EmbeddedPython: runtime pronto.");
            }
            catch (Exception ex)
            {
                _failed = true;
                AuraLog.Exception("EmbeddedPython.EnsureReady", ex);
            }
#else
            _failed = true;
#endif
        }
        finally
        {
            _initGate.Release();
        }
    }

    public async Task<string> RunCodeAsync(string code, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(code))
            return string.Empty;

        await EnsureReadyAsync(ct).ConfigureAwait(false);
        if (_env is null)
            throw new InvalidOperationException("Python embutido indisponível neste dispositivo/APK.");

        await _runGate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            string wrapped =
                "import sys, io\n" +
                "_aura_buf = io.StringIO()\n" +
                "_aura_old_stdout = sys.stdout\n" +
                "sys.stdout = _aura_buf\n" +
                "try:\n" +
                Indent(code) +
                "\nfinally:\n" +
                "    sys.stdout = _aura_old_stdout\n" +
                "_aura_buf.getvalue()\n";

            object? raw = await InvokeRunAsync(_env, wrapped).ConfigureAwait(false);
            return raw?.ToString() ?? string.Empty;
        }
        finally
        {
            _runGate.Release();
        }
    }

    public async Task<string> RunFileAsync(string filePath, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            throw new FileNotFoundException("Arquivo Python não encontrado.", filePath);

        string code = await File.ReadAllTextAsync(filePath, ct).ConfigureAwait(false);
        return await RunCodeAsync(code, ct).ConfigureAwait(false);
    }

    private static async Task<object?> InvokeRunAsync(object env, string code)
    {
        var t = env.GetType();
        var method = t.GetMethod("RunCode", new[] { typeof(string) })
            ?? t.GetMethod("Run", new[] { typeof(string) })
            ?? t.GetMethods().FirstOrDefault(x =>
                x.Name.Contains("Run", StringComparison.OrdinalIgnoreCase)
                && x.GetParameters().Length == 1
                && x.GetParameters()[0].ParameterType == typeof(string));

        if (method is null)
            throw new MissingMethodException(t.FullName, "RunCode");

        object? result = method.Invoke(env, new object[] { code });
        if (result is Task task)
        {
            await task.ConfigureAwait(false);
            return task.GetType().GetProperty("Result")?.GetValue(task);
        }

        return result;
    }

    private static string Indent(string code)
    {
        var lines = code.Replace("\r\n", "\n").Split('\n');
        return string.Join("\n", lines.Select(l => "    " + l));
    }
}
