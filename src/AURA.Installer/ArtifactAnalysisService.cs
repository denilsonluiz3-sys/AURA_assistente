namespace AURA.Installer;

/// <summary>
/// Resultado combinado das Etapas 1 e 2 do pipeline do Instalador Inteligente.
/// </summary>
public sealed class ArtifactAnalysisResult
{
    public required ArtifactIdentification Identification { get; init; }

    /// <summary>Null quando ainda não existe IDependencyAnalyzer registrado para o tipo identificado.</summary>
    public DependencyReport? Dependencies { get; init; }
}

/// <summary>
/// Orquestra Etapa 1 (Identificação) + Etapa 2 (Análise de Dependências).
/// As etapas 3 a 7 (ambiente, instalação, configuração, execução,
/// gerenciamento) entram como novos métodos/serviços nas próximas fases,
/// sem precisar mexer nisso aqui.
/// </summary>
public sealed class ArtifactAnalysisService
{
    private readonly IFileIdentifier _identifier;
    private readonly IReadOnlyDictionary<ArtifactType, IDependencyAnalyzer> _analyzers;

    public ArtifactAnalysisService(IFileIdentifier identifier, IEnumerable<IDependencyAnalyzer> analyzers)
    {
        _identifier = identifier;
        _analyzers = analyzers.ToDictionary(a => a.SupportedType);
    }

    /// <summary>Construtor de conveniência já com o identificador e os analisadores padrão da AURA.</summary>
    public static ArtifactAnalysisService CreateDefault()
    {
        return new ArtifactAnalysisService(
            new FileIdentifier(),
            new IDependencyAnalyzer[] { new PythonDependencyAnalyzer() });
    }

    public async Task<ArtifactAnalysisResult> AnalyzeAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var identification = await _identifier.IdentifyAsync(filePath, cancellationToken);

        if (!_analyzers.TryGetValue(identification.Type, out var analyzer))
        {
            return new ArtifactAnalysisResult { Identification = identification, Dependencies = null };
        }

        var dependencies = await analyzer.AnalyzeAsync(filePath, cancellationToken);
        return new ArtifactAnalysisResult { Identification = identification, Dependencies = dependencies };
    }
}
