namespace AURA.Installer;

/// <summary>
/// Etapa 2 do pipeline: dado um artefato já identificado, descobre do que
/// ele precisa para rodar (pacotes, runtimes). Uma implementação por
/// ArtifactType.
/// </summary>
public interface IDependencyAnalyzer
{
    ArtifactType SupportedType { get; }

    Task<DependencyReport> AnalyzeAsync(string filePath, CancellationToken cancellationToken = default);
}
