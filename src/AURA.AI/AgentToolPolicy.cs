namespace AURA.AI;

/// <summary>
/// Capacidade efetiva de uma sessão. A política é verificada antes de expor
/// ferramentas ao modelo e novamente antes de executar cada chamada.
/// </summary>
public sealed class AgentToolPolicy
{
    private readonly HashSet<string> _allowedTools;

    public AgentToolPolicy(IEnumerable<string> allowedTools, bool requireConfirmation = false)
    {
        ArgumentNullException.ThrowIfNull(allowedTools);
        _allowedTools = new HashSet<string>(allowedTools.Where(x => !string.IsNullOrWhiteSpace(x)), StringComparer.OrdinalIgnoreCase);
        RequireConfirmation = requireConfirmation;
    }

    public bool RequireConfirmation { get; }

    public bool Allows(string? toolName) =>
        !string.IsNullOrWhiteSpace(toolName) && _allowedTools.Contains(toolName);

    public IReadOnlyList<AgentToolDefinition> Filter(IEnumerable<AgentToolDefinition> definitions) =>
        definitions.Where(definition => Allows(definition.Name)).ToArray();
}
