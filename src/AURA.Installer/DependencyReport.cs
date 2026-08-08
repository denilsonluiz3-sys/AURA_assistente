namespace AURA.Installer;

/// <summary>
/// Resultado da Etapa 2 (Análise de Dependências) do pipeline do Instalador
/// Inteligente. "Dependencies" já vem no formato pronto pra Etapa 4
/// (nome do pacote pip/npm/maven), não o nome bruto do módulo importado.
/// </summary>
public sealed class DependencyReport
{
    public ArtifactType ArtifactType { get; set; }

    /// <summary>Pacotes a instalar (ex.: "opencv-python", "requests").</summary>
    public List<string> Dependencies { get; set; } = new();

    /// <summary>Imports/símbolos encontrados mas que não conseguimos mapear com confiança para um pacote.</summary>
    public List<string> UnresolvedImports { get; set; } = new();

    public bool HasRequirementsFile { get; set; }

    public string? RequirementsFilePath { get; set; }

    public List<string> Notes { get; set; } = new();
}
