namespace AURA.Abstractions.Execution;

/// <summary>
/// Interpretador Python embutido no APK (não depende de Termux/PATH).
/// </summary>
public interface IEmbeddedPython
{
    /// <summary>True após inicialização bem-sucedida.</summary>
    bool IsReady { get; }

    /// <summary>Extrai/prepara o runtime (uma vez por sessão).</summary>
    Task EnsureReadyAsync(CancellationToken ct = default);

    /// <summary>Executa código Python e devolve stdout capturado.</summary>
    Task<string> RunCodeAsync(string code, CancellationToken ct = default);

    /// <summary>Executa um arquivo .py (lê o conteúdo e roda).</summary>
    Task<string> RunFileAsync(string filePath, CancellationToken ct = default);
}
