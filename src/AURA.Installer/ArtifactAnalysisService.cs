namespace AURA.Installer;

/// <summary>
/// Resultado combinado das Etapas 1, 2 e (opcionalmente) 3 do pipeline do
/// Instalador Inteligente.
/// </summary>
public sealed class ArtifactAnalysisResult
{
    public required ArtifactIdentification Identification { get; init; }

    /// <summary>Null quando ainda não existe IDependencyAnalyzer registrado para o tipo identificado.</summary>
    public DependencyReport? Dependencies { get; init; }

    /// <summary>Null quando a Etapa 3 não foi pedida, ou quando ainda não existe IEnvironmentSelector pro tipo.</summary>
    public EnvironmentSelectionResult? Environment { get; init; }
}

/// <summary>
/// Orquestra Etapa 1 (Identificação) + Etapa 2 (Análise de Dependências) +,
/// sob demanda, Etapa 3 (Escolha do Ambiente). As etapas 4 a 7 (instalação,
/// configuração, execução, gerenciamento) entram como novos métodos/serviços
/// nas próximas fases, sem precisar mexer nisso aqui.
/// </summary>
public sealed class ArtifactAnalysisService
{
    private readonly IFileIdentifier _identifier;
    private readonly IReadOnlyDictionary<ArtifactType, IDependencyAnalyzer> _analyzers;
    private readonly EnvironmentSelectionService _environmentService;

    public ArtifactAnalysisService(
        IFileIdentifier identifier,
        IEnumerable<IDependencyAnalyzer> analyzers,
        EnvironmentSelectionService? environmentService = null)
    {
        _identifier = identifier;
        _analyzers = analyzers.ToDictionary(a => a.SupportedType);
        _environmentService = environmentService ?? EnvironmentSelectionService.CreateDefault();
    }

    /// <summary>Construtor de conveniência já com o identificador e os analisadores padrão da AURA.</summary>
    public static ArtifactAnalysisService CreateDefault()
    {
        return new ArtifactAnalysisService(
            new FileIdentifier(),
            new IDependencyAnalyzer[] { new PythonDependencyAnalyzer() });
    }

    /// <summary>Roda só as Etapas 1 e 2 (identificação + dependências), sem checar o ambiente.</summary>
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

    /// <summary>Roda as Etapas 1, 2 e 3 em sequência: identifica, analisa dependências e checa o ambiente.</summary>
    public async Task<ArtifactAnalysisResult> AnalyzeWithEnvironmentAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var partial = await AnalyzeAsync(filePath, cancellationToken);

        if (partial.Dependencies is null)
        {
            return partial; // sem Etapa 2 pro tipo, não dá pra checar o ambiente com base em dependências.
        }

        var environment = await _environmentService.SelectAsync(partial.Identification.Type, partial.Dependencies, cancellationToken);
        return new ArtifactAnalysisResult
        {
            Identification = partial.Identification,
            Dependencies = partial.Dependencies,
            Environment = environment
        };
    }
}
