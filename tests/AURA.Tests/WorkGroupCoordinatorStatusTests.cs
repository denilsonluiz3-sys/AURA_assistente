using AURA.Agents.Workgroups;
using Xunit;

namespace AURA.Tests;

public sealed class WorkGroupCoordinatorStatusTests
{
    [Fact]
    public async Task PartialReportKeepsWorkInProgress()
    {
        var registry = WorkGroupRegistry.CreateDefault();
        var coordinator = new WorkGroupCoordinator(registry);
        coordinator.Register(new FixedAgent("ia-offline", AgentReportStatus.Partial));

        AgentReport report = await coordinator.AnalyzeAsync("validar runtime GGUF");

        Assert.Equal(AgentReportStatus.Partial, report.Status);
    }

    [Fact]
    public async Task SpecialistFailureReturnsFailedReportInsteadOfLeavingUncaughtState()
    {
        var coordinator = new WorkGroupCoordinator(WorkGroupRegistry.CreateDefault());
        coordinator.Register(new ThrowingAgent("ia-offline"));

        AgentReport report = await coordinator.AnalyzeAsync("validar runtime GGUF");

        Assert.Equal(AgentReportStatus.Failed, report.Status);
        Assert.Contains("não concluiu", report.Findings[0]);
    }

    private sealed class FixedAgent(string groupId, AgentReportStatus status) : IWorkGroupAgent
    {
        public string GroupId { get; } = groupId;
        public Task<AgentReport> AnalyzeAsync(WorkItem item, WorkGroupDefinition group, CancellationToken cancellationToken = default) =>
            Task.FromResult(new AgentReport { WorkItemId = item.Id, GroupId = group.Id, Objective = item.Objective, Status = status });
    }

    private sealed class ThrowingAgent(string groupId) : IWorkGroupAgent
    {
        public string GroupId { get; } = groupId;
        public Task<AgentReport> AnalyzeAsync(WorkItem item, WorkGroupDefinition group, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("test failure");
    }
}
