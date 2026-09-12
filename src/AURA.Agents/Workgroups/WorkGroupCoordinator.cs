using AURA.Agents.Specialists;
using AURA.AI;

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
        if (_registry.Resolve("ia-offline") != null)
            Register(new OfflineAiPlanningAgent());
    }

    public AgentReport? LastReport { get; private set; }

    public void Register(IWorkGroupAgent agent)
    {
        ArgumentNullException.ThrowIfNull(agent);
        WorkGroupDefinition? group = _registry.Resolve(agent.GroupId);
        if (group == null)
            throw new InvalidOperationException("Grupo não registrado: " + agent.GroupId);
        if (!group.Enabled)
            throw new InvalidOperationException("Grupo desativado: " + agent.GroupId);
        _agents[agent.GroupId] = agent;
    }

    public AgentToolPolicy CreateToolPolicy(WorkItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        WorkGroupDefinition? group = _registry.Resolve(item.GroupId);
        if (group == null)
            throw new InvalidOperationException("Grupo não registrado: " + item.GroupId);
        if (!group.Enabled)
            throw new InvalidOperationException("Grupo desativado: " + item.GroupId);
        if (!group.AllowedStages.Contains(item.Stage))
            throw new InvalidOperationException("Estágio não autorizado para o grupo: " + item.Stage);
        return new AgentToolPolicy(group.AllowedTools);
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
        _ = CreateToolPolicy(item);
        await SaveItemAsync(item, cancellationToken).ConfigureAwait(false);

        AgentReport report;
        try
        {
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

            if (report.WorkItemId != item.Id || report.GroupId != group.Id)
            {
                report = FailureReport(item, group, "O agente retornou um relatório vinculado a outro trabalho ou grupo.");
            }
        }
        catch (OperationCanceledException)
        {
            item.Status = WorkItemStatus.Pending;
            item.UpdatedAtUtc = DateTime.UtcNow;
            await SaveItemAsync(item, CancellationToken.None).ConfigureAwait(false);
            throw;
        }
        catch (Exception ex)
        {
            report = FailureReport(item, group, "O agente não concluiu a análise: " + ex.GetType().Name);
        }

        item.Status = report.Status switch
        {
            AgentReportStatus.Complete => WorkItemStatus.Completed,
            AgentReportStatus.Partial => WorkItemStatus.InProgress,
            AgentReportStatus.Blocked or AgentReportStatus.Failed => WorkItemStatus.Blocked,
            _ => WorkItemStatus.Blocked
        };
        item.UpdatedAtUtc = DateTime.UtcNow;
        await SaveItemAsync(item, cancellationToken).ConfigureAwait(false);

        LastReport = report;
        if (_reports != null)
            await _reports.SaveAsync(report, cancellationToken).ConfigureAwait(false);
        return report;
    }

    private static AgentReport FailureReport(WorkItem item, WorkGroupDefinition group, string reason) =>
        new()
        {
            WorkItemId = item.Id,
            GroupId = group.Id,
            Objective = item.Objective,
            Status = AgentReportStatus.Failed,
            Findings = new[] { reason },
            Recommendation = "Revisar o erro antes de tentar novamente.",
            NextStep = "Corrigir o agente ou o contexto do trabalho.",
            Confidence = 1
        };

    private Task SaveItemAsync(WorkItem item, CancellationToken ct) =>
        _items == null ? Task.CompletedTask : _items.SaveAsync(item, ct);
}
