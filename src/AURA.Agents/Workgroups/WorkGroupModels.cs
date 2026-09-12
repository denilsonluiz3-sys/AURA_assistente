namespace AURA.Agents.Workgroups;

public enum WorkGroupStage
{
    Observe,
    Prepare,
    ExecuteControlled,
    Publish
}

public enum WorkItemStatus
{
    Pending,
    InProgress,
    Blocked,
    Completed,
    Cancelled
}

public enum AgentReportStatus
{
    Partial,
    Complete,
    Blocked,
    Failed
}

public sealed class WorkGroupDefinition
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Mission { get; init; } = string.Empty;
    public IReadOnlyList<string> AllowedTools { get; init; } = Array.Empty<string>();
    public IReadOnlyList<WorkGroupStage> AllowedStages { get; init; } = new[] { WorkGroupStage.Observe };
    public bool Enabled { get; init; } = true;
}

public sealed class WorkItem
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string GroupId { get; init; } = string.Empty;
    public string Objective { get; init; } = string.Empty;
    public string? Context { get; init; }
    public WorkGroupStage Stage { get; set; } = WorkGroupStage.Observe;
    public WorkItemStatus Status { get; set; } = WorkItemStatus.Pending;
    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class AgentEvidence
{
    public string Type { get; init; } = string.Empty;
    public string Reference { get; init; } = string.Empty;
    public string Observation { get; init; } = string.Empty;
    public double Confidence { get; init; }
}

public sealed class AgentReport
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string WorkItemId { get; init; } = string.Empty;
    public string GroupId { get; init; } = string.Empty;
    public AgentReportStatus Status { get; init; } = AgentReportStatus.Partial;
    public string Objective { get; init; } = string.Empty;
    public IReadOnlyList<string> Findings { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Risks { get; init; } = Array.Empty<string>();
    public IReadOnlyList<AgentEvidence> Evidence { get; init; } = Array.Empty<AgentEvidence>();
    public string? Recommendation { get; init; }
    public string? NextStep { get; init; }
    public double Confidence { get; init; }
    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
}

public sealed class WorkGroupRegistry
{
    private readonly Dictionary<string, WorkGroupDefinition> _groups = new(StringComparer.OrdinalIgnoreCase);

    public int Count => _groups.Count;

    public void Register(WorkGroupDefinition group)
    {
        ArgumentNullException.ThrowIfNull(group);
        if (string.IsNullOrWhiteSpace(group.Id))
            throw new ArgumentException("O grupo precisa de um Id.", nameof(group));
        if (string.IsNullOrWhiteSpace(group.Name))
            throw new ArgumentException("O grupo precisa de um nome.", nameof(group));
        string id = group.Id.Trim();
        if (_groups.ContainsKey(id))
            throw new InvalidOperationException("Grupo já registrado: " + id);
        _groups[id] = group;
    }

    public WorkGroupDefinition? Resolve(string id) =>
        !string.IsNullOrWhiteSpace(id) && _groups.TryGetValue(id.Trim(), out var group) ? group : null;

    public IReadOnlyList<WorkGroupDefinition> List() => _groups.Values
        .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
        .ToArray();

    public WorkGroupDefinition ResolveFor(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Resolve("conhecimento") ?? throw new InvalidOperationException("Grupo conhecimento não registrado.");

        string value = text.ToLowerInvariant();
        string? id = value switch
        {
            _ when ContainsAny(value, "offline", "gguf", "llama", "modelo local", "ndk") => "ia-offline",
            _ when ContainsAny(value, "tool call", "ferramenta", "memória", "prompt", "agente") => "ia-toolcalls",
            _ when ContainsAny(value, "segurança", "permissão", "permissões", "privacidade", "risco") => "seguranca",
            _ when ContainsAny(value, "tela", "ux", "interface", "navegação", "acessibilidade") => "ux",
            _ when ContainsAny(value, "tarefa", "lembrete", "notificação") => "tarefas",
            _ when ContainsAny(value, "teste", "ci", "build", "apk", "workflow") => "qa-ci",
            _ when ContainsAny(value, "arquitetura", "dependência", "contrato", "duplicidade") => "arquitetura",
            _ when ContainsAny(value, "produto", "usuário", "prioridade", "utilidade") => "produto",
            _ => "conhecimento"
        };

        return Resolve(id) ?? Resolve("conhecimento")!;
    }

    private static bool ContainsAny(string value, params string[] terms) =>
        terms.Any(value.Contains);

    public static WorkGroupRegistry CreateDefault()
    {
        var registry = new WorkGroupRegistry();
        string[] readOnly = { "read_file", "list_dir", "search_files" };
        registry.Register(new WorkGroupDefinition { Id = "produto", Name = "Produto e utilidade", Mission = "Avaliar valor para o usuário comum e priorizar funções.", AllowedTools = readOnly });
        registry.Register(new WorkGroupDefinition { Id = "arquitetura", Name = "Arquitetura", Mission = "Analisar estrutura, dependências, contratos e duplicidades.", AllowedTools = readOnly });
        registry.Register(new WorkGroupDefinition { Id = "ia-toolcalls", Name = "IA e tool calls", Mission = "Avaliar contexto, memória, tool calls e respostas do Agente.", AllowedTools = readOnly });
        registry.Register(new WorkGroupDefinition { Id = "ia-offline", Name = "IA offline", Mission = "Analisar modelos locais, runtime, GGUF e execução sem rede.", AllowedTools = readOnly });
        registry.Register(new WorkGroupDefinition { Id = "seguranca", Name = "Segurança", Mission = "Revisar permissões, dados, execução e riscos de segurança.", AllowedTools = readOnly });
        registry.Register(new WorkGroupDefinition { Id = "ux", Name = "Experiência", Mission = "Analisar clareza, navegação, acessibilidade e fluxos.", AllowedTools = readOnly });
        registry.Register(new WorkGroupDefinition { Id = "tarefas", Name = "Tarefas e lembretes", Mission = "Projetar tarefas, lembretes e notificações locais.", AllowedTools = readOnly });
        registry.Register(new WorkGroupDefinition { Id = "qa-ci", Name = "QA e CI", Mission = "Definir testes, validações, builds e critérios de aceite.", AllowedTools = readOnly });
        registry.Register(new WorkGroupDefinition { Id = "conhecimento", Name = "Conhecimento", Mission = "Consolidar evidências, decisões e padrões reutilizáveis.", AllowedTools = readOnly });
        return registry;
    }
}
