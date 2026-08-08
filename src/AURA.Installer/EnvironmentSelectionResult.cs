using AURA.SystemInfo;

namespace AURA.Installer;

/// <summary>
/// Resultado da Etapa 3 (Escolha do Ambiente) do pipeline do Instalador
/// Inteligente: o runtime necessário já está disponível? E o disco aguenta
/// instalar as dependências encontradas na Etapa 2?
/// </summary>
public sealed class EnvironmentSelectionResult
{
    public ArtifactType ArtifactType { get; set; }

    public bool RuntimeAvailable { get; set; }

    /// <summary>Nome do binário do runtime resolvido (ex.: "python3"), ou null se não encontrado.</summary>
    public string? RuntimeBinary { get; set; }

    /// <summary>Comandos sugeridos pra instalar o runtime, adequados ao ambiente detectado (Termux/Linux/Windows/macOS).</summary>
    public List<string> InstallRuntimeSuggestions { get; set; } = new();

    public SystemDiagnosticsResult SystemDiagnostics { get; set; } = null!;

    /// <summary>Estimativa grosseira de espaço necessário pras dependências da Etapa 2, em MB.</summary>
    public double EstimatedDependenciesDiskMb { get; set; }

    public bool HasEnoughDiskSpace { get; set; }

    public List<string> Warnings { get; set; } = new();

    /// <summary>True quando dá pra seguir direto pra Etapa 4 (Instalação) sem intervenção do usuário.</summary>
    public bool ReadyToInstall => RuntimeAvailable && HasEnoughDiskSpace;
}
