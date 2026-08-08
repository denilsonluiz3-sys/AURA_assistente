using AURA.Abstractions.Execution;
using AURA.Modules.Executors;

namespace AURA.Installer;

/// <summary>
/// Instala dependências Python via "python -m pip install ..." reaproveitando
/// o PythonExecutor já existente (evita duplicar lógica de resolução de
/// binário/timeout/captura de saída). Por padrão roda em dry-run: só monta
/// o comando e devolve o que seria executado, sem tocar em nada.
/// </summary>
public sealed class PythonInstaller : IInstaller
{
    private readonly IToolExecutor _pythonExecutor;

    public PythonInstaller() : this(new PythonExecutor()) { }

    /// <summary>Construtor para testes: permite injetar um executor falso.</summary>
    public PythonInstaller(IToolExecutor pythonExecutor)
    {
        _pythonExecutor = pythonExecutor;
    }

    public ArtifactType SupportedType => ArtifactType.Python;

    public async Task<InstallationResult> InstallAsync(DependencyReport dependencies, bool dryRun = true, CancellationToken cancellationToken = default)
    {
        if (dependencies.Dependencies.Count == 0)
        {
            return InstallationResult.NothingToInstall(ArtifactType.Python, dryRun);
        }

        string commandText = $"python -m pip install {string.Join(" ", dependencies.Dependencies)}";

        var result = new InstallationResult
        {
            ArtifactType = ArtifactType.Python,
            DryRun = dryRun,
            Commands = { commandText }
        };

        if (dryRun)
        {
            result.Success = true;
            result.Notes.Add("[SIMULAÇÃO] Nenhum comando foi executado de verdade. Chame com dryRun: false pra instalar.");
            return result;
        }

        if (!_pythonExecutor.IsAvailable())
        {
            result.Success = false;
            result.StandardError = "Python não encontrado no ambiente (tentado: python3, python).";
            result.Notes.Add("Rode a Etapa 3 (escolha do ambiente) antes de instalar de verdade.");
            return result;
        }

        var request = new ExecutionRequest
        {
            Command = "-m",
            Arguments = new List<string> { "pip", "install" }.Concat(dependencies.Dependencies).ToList(),
            Timeout = TimeSpan.FromMinutes(5)
        };

        var execResult = await _pythonExecutor.ExecuteAsync(request, cancellationToken);

        result.Success = execResult.Success;
        result.StandardOutput = execResult.StandardOutput;
        result.StandardError = execResult.StandardError;

        if (!execResult.Success)
        {
            result.Notes.Add($"pip install terminou com código {execResult.ExitCode}.");
        }

        return result;
    }
}
