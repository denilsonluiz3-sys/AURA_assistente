namespace AURA.Installer;

/// <summary>
/// Etapa 1 do pipeline: descobre que tipo de artefato foi entregue à AURA
/// (Python, Jar, DLL, ...) a partir da assinatura binária, extensão e,
/// quando necessário, uma espiada no conteúdo.
/// </summary>
public interface IFileIdentifier
{
    Task<ArtifactIdentification> IdentifyAsync(string filePath, CancellationToken cancellationToken = default);
}
