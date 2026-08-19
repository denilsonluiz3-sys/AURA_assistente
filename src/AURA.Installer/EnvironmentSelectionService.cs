namespace AURA.Installer;

/// <summary>
/// Orquestra a Etapa 3 (Escolha do Ambiente) sozinha — útil pra quem já tem
/// um DependencyReport da Etapa 2 em mãos e só quer saber se o ambiente
/// aguenta seguir. Para rodar as três etapas em sequência a partir do
/// arquivo bruto, use <see cref="ArtifactAnalysisService.AnalyzeWithEnvironmentAsync"/>.
/// </summary>
public sealed class EnvironmentSelectionService
{
    private readonly IReadOnlyDictionary<ArtifactType, IEnvironmentSelector> _selectors;

    public EnvironmentSelectionService(IEnumerable<IEnvironmentSelector> selectors)
    {
        _selectors = selectors.ToDictionary(s => s.SupportedType);
    }

    public static EnvironmentSelectionService CreateDefault()
    {
        return new EnvironmentSelectionService(new IEnvironmentSelector[] { new PythonEnvironmentSelector() });
    }

    /// <summary>Null quando ainda não existe IEnvironmentSelector registrado para o tipo.</summary>
    public async Task<EnvironmentSelectionResult?> SelectAsync(ArtifactType type, DependencyReport dependencies, CancellationToken cancellationToken = default)
    {
        if (!_selectors.TryGetValue(type, out var selector))
        {
            return null;
        }

        return await selector.SelectAsync(dependencies, cancellationToken);
    }
}
