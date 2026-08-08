namespace AURA.Installer;

/// <summary>
/// Orquestra a Etapa 4 (Instalação) por tipo de artefato. Espelha o mesmo
/// padrão de EnvironmentSelectionService: dicionário por ArtifactType,
/// CreateDefault() com o que já está implementado.
/// </summary>
public sealed class InstallationService
{
    private readonly IReadOnlyDictionary<ArtifactType, IInstaller> _installers;

    public InstallationService(IEnumerable<IInstaller> installers)
    {
        _installers = installers.ToDictionary(i => i.SupportedType);
    }

    public static InstallationService CreateDefault()
    {
        return new InstallationService(new IInstaller[] { new PythonInstaller() });
    }

    /// <summary>Null quando ainda não existe IInstaller registrado para o tipo.</summary>
    public async Task<InstallationResult?> InstallAsync(ArtifactType type, DependencyReport dependencies, bool dryRun = true, CancellationToken cancellationToken = default)
    {
        if (!_installers.TryGetValue(type, out var installer))
        {
            return null;
        }

        return await installer.InstallAsync(dependencies, dryRun, cancellationToken);
    }
}
