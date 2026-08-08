namespace AURA.Installer;

/// <summary>
/// Etapa 4 do pipeline: instala (ou simula a instalação de) as dependências
/// encontradas na Etapa 2. dryRun=true por padrão em todo o pipeline —
/// instalação real é sempre um opt-in explícito de quem chama.
/// </summary>
public interface IInstaller
{
    ArtifactType SupportedType { get; }

    Task<InstallationResult> InstallAsync(DependencyReport dependencies, bool dryRun = true, CancellationToken cancellationToken = default);
}
