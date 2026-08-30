using AURA.Abstractions.Execution;
using AURA.Mobile.Diagnostics;
using Python3Android;

namespace AURA.Mobile.Services;

/// <summary>
/// Runtime Python 3.11 embutido no APK via Python3Android.
/// Não depende de Termux, PATH ou de um executável python3 exposto pelo shell Android.
/// </summary>
public sealed class EmbeddedPythonService : IEmbeddedPython
{
    private readonly SemaphoreSlim _initGate = new(1, 1);
    private readonly SemaphoreSlim _runGate = new(1, 1);
    private AndroidPythonEnvironment? _env;
    private Exception? _initializationError;

    public bool IsReady => _env is not null;

    public async Task EnsureReadyAsync(CancellationToken ct = default)
    {
        if (_env is not null)
            return;

        await _initGate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (_env is not null)
                return;

#if ANDROID
            try
            {
                ct.ThrowIfCancellationRequested();
                _env = await AndroidPythonEnvironment.Create().ConfigureAwait(false);
                _initializationError = null;
                AuraLog.Info("EmbeddedPython: Python3Android runtime pronto.");
            }
            catch (Exception ex)
            {
                _initializationError = ex;
                _env = null;
                AuraLog.Exception("EmbeddedPython.EnsureReady", ex);
            }
#else
            _initializationError = new PlatformNotSupportedException(
                "Python embutido está disponível somente no Android.");
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
        {
            string detail = _initializationError?.Message ?? "runtime não inicializado";
            throw new InvalidOperationException(
                $"Python embutido indisponível: {detail}", _initializationError);
        }

        await _runGate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            ct.ThrowIfCancellationRequested();

            // Python3Android expõe stdout, erro e exit code por parâmetros de saída.
            string output = _env.RunCode(code, out string error, out int exitCode) ?? string.Empty;

            if (exitCode != 0)
            {
                string detail = string.IsNullOrWhiteSpace(error)
                    ? $"Python terminou com exit code {exitCode}."
                    : error;
                throw new InvalidOperationException(detail);
            }

            return output;
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
}
