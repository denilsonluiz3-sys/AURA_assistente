namespace AURA.Installer;

/// <summary>
/// Resultado da Etapa 1 (Identificação) do pipeline do Instalador Inteligente.
/// </summary>
public sealed class ArtifactIdentification
{
    public string FilePath { get; set; } = string.Empty;

    public ArtifactType Type { get; set; } = ArtifactType.Unknown;

    /// <summary>Confiança da identificação, de 0.0 (nenhuma pista) a 1.0 (certeza).</summary>
    public double Confidence { get; set; }

    /// <summary>Observações sobre como o tipo foi determinado (assinatura, extensão, conteúdo).</summary>
    public List<string> Notes { get; set; } = new();

    public static ArtifactIdentification Unrecognized(string filePath, string reason)
    {
        var result = new ArtifactIdentification
        {
            FilePath = filePath,
            Type = ArtifactType.Unknown,
            Confidence = 0.0
        };
        result.Notes.Add(reason);
        return result;
    }
}
