using AURA.Agents.Specialists;

namespace AURA.Agents.Workgroups;

public interface IWorkGroupAgent
{
    string GroupId { get; }
    Task<AgentReport> AnalyzeAsync(
        WorkItem workItem,
        WorkGroupDefinition group,
        CancellationToken cancellationToken = default);
}

/// <summary>Coordena a etapa inicial de observação dos grupos especializados.</summary>
public sealed class WorkGroupCoordinator
{
    private readonly WorkGroupRegistry _registry;
    private readonly AgentReportStore? _reports;
    private readonly WorkItemStore? _items;
    private readonly Dictionary<string, IWorkGroupAgent> _agents = new(StringComparer.OrdinalIgnoreCase);

    public WorkGroupCoordinator(
        WorkGroupRegistry registry,
        AgentReportStore? reports = null,
        WorkItemStore? items = null)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _reports = reports;
        _items = items;
        if (_registry.Resolve("qa-ci") != null)
            Register(new QaCiPlanningAgent());
    }

    public AgentReport? LastReport { get; private set; }

    public void Register(IWorkGroupAgent agent)
    {
        ArgumentNullException.ThrowIfNull(agent);
        if (_registry.Resolve(agent.GroupId) == null)
            throw new InvalidOperationException("Grupo não registrado: " + agent.GroupId);
        _agents[agent.GroupId] = agent;
    }

    public async Task<AgentReport> AnalyzeAsync(
        string objective,
        string? context = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(objective))
            throw new ArgumentException("Objetivo obrigatório.", nameof(objective));

        WorkGroupDefinition group = _registry.ResolveFor(objective);
        var item = new WorkItem
        {
            GroupId = group.Id,
            Objective = objective.Trim(),
            Context = context,
            Stage = WorkGroupStage.Observe,
            Status = WorkItemStatus.InProgress
        };
        await SaveItemAsync(item, cancellationToken).ConfigureAwait(false);

        AgentReport report;
        if (_agents.TryGetValue(group.Id, out IWorkGroupAgent? agent))
        {
            report = await agent.AnalyzeAsync(item, group, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            report = new AgentReport
            {
                WorkItemId = item.Id,
                GroupId = group.Id,
                Objective = item.Objective,
                Status = AgentReportStatus.Partial,
                Findings = new[] { "Solicitação roteada para o grupo especializado." },
                Recommendation = "Conectar um agente especializado para aprofundar a observação.",
                NextStep = "Executar análise específica do grupo.",
                Confidence = 0.5
            };
        }

        item.Status = report.Status == AgentReportStatus.Failed
            ? WorkItemStatus.Blocked
            : WorkItemStatus.Completed;
        item.UpdatedAtUtc = DateTime.UtcNow;
        await SaveItemAsync(item, cancellationToken).ConfigureAwait(false);

        LastReport = report;
        if (_reports != null)
            await _reports.SaveAsync(report, cancellationToken).ConfigureAwait(false);
        return report;
    }

    private Task SaveItemAsync(WorkItem item, CancellationToken ct) =>
        _items == null ? Task.CompletedTask : _items.SaveAsync(item, ct);
}
