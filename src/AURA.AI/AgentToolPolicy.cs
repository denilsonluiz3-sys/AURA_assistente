namespace AURA.AI;

public enum PermissionDecision
{
    AllowOnce,
    AllowAlways,
    Deny,
    DenyAlways
}

/// <summary>Capacidade efetiva de uma sessão e autorização individual por permissão.</summary>
public sealed class AgentToolPolicy
{
    private readonly HashSet<string> _allowedTools;
    private readonly HashSet<string> _alwaysAllowed = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _alwaysDenied = new(StringComparer.OrdinalIgnoreCase);

    public AgentToolPolicy(IEnumerable<string> allowedTools, bool requireConfirmation = false)
    {
        ArgumentNullException.ThrowIfNull(allowedTools);
        _allowedTools = new HashSet<string>(allowedTools.Where(x => !string.IsNullOrWhiteSpace(x)), StringComparer.OrdinalIgnoreCase);
        RequireConfirmation = requireConfirmation;
    }

    public bool RequireConfirmation { get; }
    public Func<string, CancellationToken, Task<PermissionDecision>>? PermissionPrompt { get; set; }

    public bool Allows(string? toolName) =>
        !string.IsNullOrWhiteSpace(toolName) && _allowedTools.Contains(toolName);

    public IReadOnlyList<AgentToolDefinition> Filter(IEnumerable<AgentToolDefinition> definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);
        // Com uma pergunta configurada, o modelo pode solicitar uma capacidade bloqueada;
        // a execução continua protegida por AuthorizeAsync antes da ferramenta rodar.
        return PermissionPrompt != null
            ? definitions.ToArray()
            : definitions.Where(definition => Allows(definition.Name)).ToArray();
    }

    public async Task<bool> AuthorizeAsync(string? toolName, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(toolName)) return false;
        if (Allows(toolName) || _alwaysAllowed.Contains(toolName)) return true;
        if (_alwaysDenied.Contains(toolName)) return false;
        if (PermissionPrompt == null) return false;

        PermissionDecision decision = await PermissionPrompt(toolName, ct).ConfigureAwait(false);
        if (decision == PermissionDecision.AllowAlways)
            _alwaysAllowed.Add(toolName);
        else if (decision == PermissionDecision.DenyAlways)
            _alwaysDenied.Add(toolName);
        return decision is PermissionDecision.AllowOnce or PermissionDecision.AllowAlways;
    }
}
