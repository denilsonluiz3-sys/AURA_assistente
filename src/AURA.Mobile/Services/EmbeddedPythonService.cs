using AURA.Abstractions.Execution;
using AURA.Mobile.Diagnostics;

namespace AURA.Mobile.Services;

/// <summary>
/// Interpretador Python embutido via NuGet Python3Android.
/// Fallback seguro: se o pacote falhar no device, IsReady permanece false.
/// </summary>
public sealed class EmbeddedPythonService : IEmbeddedPython
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private object? _env; // AndroidPythonEnvironment
    private bool _failed;

    public bool IsReady => _env is not null && !_failed;

    public async Task EnsureReadyAsync(CancellationToken ct = default)
    {
        if (_env is not null || _failed)
            return;

        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (_env is not null || _failed)
                return;

#if ANDROID
            try
            {
                // Python3Android: AndroidPythonEnvironment.Create()
                var t = Type.GetType("Python3Android.AndroidPythonEnvironment, Python3Android")
                    ?? Type.GetType("Python3Android.AndroidPythonEnvironment, python3android");

                if (t is null)
                {
                    // fallback: assembly carregado por nome
                    foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        t = asm.GetType("Python3Android.AndroidPythonEnvironment");
                        if (t is not null) break;
                    }
                }

                if (t is null)
                {
                    AuraLog.Warning("EmbeddedPython: tipo AndroidPythonEnvironment não encontrado (pacote ausente?).");
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
                    var resultProp = task.GetType().GetProperty("Result");
                    _env = resultProp?.GetValue(task);
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
            await Task.CompletedTask;
#endif
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<string> RunCodeAsync(string code, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(code))
            return string.Empty;

        await EnsureReadyAsync(ct).ConfigureAwait(false);
        if (_env is null)
            throw new InvalidOperationException("Python embutido indisponível neste dispositivo/APK.");

        // Prefer captura de stdout via wrapper
        string wrapped =
            "import sys, io\n" +
            "_buf = io.StringIO()\n" +
            "_old = sys.stdout\n" +
            "sys.stdout = _buf\n" +
            "try:\n" +
            Indent(code) +
            "\nfinally:\n" +
            "    sys.stdout = _old\n" +
            "_result = _buf.getvalue()\n";

        object? raw = InvokeRun(_env, wrapped);
        string text = raw?.ToString() ?? string.Empty;

        // Alguns builds devolvem só o último expr; se vazio, tenta código direto
        if (string.IsNullOrWhiteSpace(text))
        {
            raw = InvokeRun(_env, code);
            text = raw?.ToString() ?? string.Empty;
        }

        return text;
    }

    public async Task<string> RunFileAsync(string filePath, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            throw new FileNotFoundException("Arquivo Python não encontrado.", filePath);

        string code = await File.ReadAllTextAsync(filePath, ct).ConfigureAwait(false);
        return await RunCodeAsync(code, ct).ConfigureAwait(false);
    }

    private static object? InvokeRun(object env, string code)
    {
        var t = env.GetType();
        var m = t.GetMethod("RunCode", new[] { typeof(string) })
            ?? t.GetMethod("Run", new[] { typeof(string) })
            ?? t.GetMethods().FirstOrDefault(x =>
                x.Name.Contains("Run", StringComparison.OrdinalIgnoreCase)
                && x.GetParameters().Length == 1
                && x.GetParameters()[0].ParameterType == typeof(string));

        if (m is null)
            throw new MissingMethodException(t.FullName, "RunCode");

        return m.Invoke(env, new object[] { code });
    }

    private static string Indent(string code)
    {
        var lines = code.Replace("\r\n", "\n").Split('\n');
        return string.Join("\n", lines.Select(l => "    " + l));
    }
}
