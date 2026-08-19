using AURA.Abstractions.Execution;
using AURA.Modules.Executors;
using AURA.SystemInfo;

namespace AURA.Installer;

/// <summary>
/// Etapa 3 para artefatos Python: reaproveita <see cref="PythonExecutor.IsAvailable"/>
/// pra checar o runtime e o <see cref="SystemAnalyzer"/> pra checar disco livre.
/// Não instala nada — só decide e sugere; a instalação de verdade é a Etapa 4.
/// </summary>
public sealed class PythonEnvironmentSelector : IEnvironmentSelector
{
    // Heurística grosseira: cada dependência pip "custa" em média ~30MB de disco
    // (pacotes puros Python são bem menores, alguns como numpy/torch são muito
    // maiores — é só uma estimativa conservadora pra dar um alerta cedo, não um
    // cálculo exato).
    private const double BaseOverheadMb = 50.0;
    private const double PerDependencyMb = 30.0;

    private readonly IToolExecutor _pythonExecutor;
    private readonly Func<SystemDiagnosticsResult> _diagnosticsProvider;

    public PythonEnvironmentSelector()
        : this(new PythonExecutor(), () => new SystemAnalyzer().Analyze())
    {
    }

    /// <summary>Construtor para testes: permite injetar um executor e diagnósticos falsos.</summary>
    public PythonEnvironmentSelector(IToolExecutor pythonExecutor, Func<SystemDiagnosticsResult> diagnosticsProvider)
    {
        _pythonExecutor = pythonExecutor;
        _diagnosticsProvider = diagnosticsProvider;
    }

    public ArtifactType SupportedType => ArtifactType.Python;

    public Task<EnvironmentSelectionResult> SelectAsync(DependencyReport dependencies, CancellationToken cancellationToken = default)
    {
        var diagnostics = _diagnosticsProvider();
        bool runtimeAvailable = _pythonExecutor.IsAvailable();

        double estimatedMb = BaseOverheadMb + (dependencies.Dependencies.Count * PerDependencyMb);
        double freeMb = diagnostics.FreeDiskSpaceGb * 1024.0;
        bool hasEnoughDisk = freeMb >= estimatedMb;

        var result = new EnvironmentSelectionResult
        {
            ArtifactType = ArtifactType.Python,
            RuntimeAvailable = runtimeAvailable,
            RuntimeBinary = runtimeAvailable ? _pythonExecutor.Name : null,
            SystemDiagnostics = diagnostics,
            EstimatedDependenciesDiskMb = estimatedMb,
            HasEnoughDiskSpace = hasEnoughDisk
        };

        if (!runtimeAvailable)
        {
            result.InstallRuntimeSuggestions.AddRange(SuggestPythonInstallCommands());
            result.Warnings.Add("Python não encontrado no ambiente (tentado: python3, python).");
        }

        if (!hasEnoughDisk)
        {
            result.Warnings.Add(
                $"Espaço livre em disco ({freeMb:F0} MB) pode não ser suficiente pra {dependencies.Dependencies.Count} dependência(s) " +
                $"(estimativa aproximada: {estimatedMb:F0} MB). Isso é uma estimativa grosseira, não uma garantia.");
        }

        if (dependencies.UnresolvedImports.Count > 0)
        {
            result.Warnings.Add(
                $"{dependencies.UnresolvedImports.Count} import(s) tiveram o pacote pip assumido por convenção (sem confirmação): " +
                string.Join(", ", dependencies.UnresolvedImports));
        }

        return Task.FromResult(result);
    }

    private static List<string> SuggestPythonInstallCommands()
    {
        bool isTermux = Environment.GetEnvironmentVariable("TERMUX_VERSION") is not null;

        if (isTermux)
        {
            return new List<string> { "pkg install python" };
        }

        if (OperatingSystem.IsWindows())
        {
            return new List<string> { "Baixe em https://www.python.org/downloads/ e marque \"Add Python to PATH\" na instalação." };
        }

        if (OperatingSystem.IsMacOS())
        {
            return new List<string> { "brew install python3" };
        }

        // Linux genérico (não-Termux).
        return new List<string>
        {
            "sudo apt install python3 python3-pip   # Debian/Ubuntu",
            "sudo dnf install python3 python3-pip   # Fedora",
            "sudo pacman -S python python-pip       # Arch"
        };
    }
}
