namespace AURA.Installer;

/// <summary>
/// Etapa 3 do pipeline: dado o relatório de dependências da Etapa 2, decide
/// se o ambiente atual tem o runtime necessário e recursos suficientes pra
/// seguir pra instalação. Uma implementação por ArtifactType.
/// </summary>
public interface IEnvironmentSelector
{
    ArtifactType SupportedType { get; }

    Task<EnvironmentSelectionResult> SelectAsync(DependencyReport dependencies, CancellationToken cancellationToken = default);
}
