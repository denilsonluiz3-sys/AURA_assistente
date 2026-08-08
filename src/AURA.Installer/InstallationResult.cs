namespace AURA.Installer;

/// <summary>
/// Resultado da Etapa 4 (Instalação) do pipeline. Serve tanto pro modo
/// simulação (DryRun=true, nada é executado) quanto pra instalação real.
/// </summary>
public sealed class InstallationResult
{
    public ArtifactType ArtifactType { get; set; }

    public bool DryRun { get; set; }

    public bool Success { get; set; }

    /// <summary>Comando(s) que foram (ou seriam, em dry-run) executados, em texto legível.</summary>
    public List<string> Commands { get; set; } = new();

    public string StandardOutput { get; set; } = string.Empty;

    public string StandardError { get; set; } = string.Empty;

    public List<string> Notes { get; set; } = new();

    public static InstallationResult NothingToInstall(ArtifactType type, bool dryRun) => new()
    {
        ArtifactType = type,
        DryRun = dryRun,
        Success = true,
        Notes = { "Nenhuma dependência a instalar." }
    };
}
