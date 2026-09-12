using AURA.Agents.Workgroups;
using Xunit;

namespace AURA.Tests;

public sealed class WorkGroupCoordinatorTests
{
    [Fact]
    public async Task AnalyzeAsyncCreatesPartialObservationWhenNoSpecialistIsRegistered()
    {
        var coordinator = new WorkGroupCoordinator(WorkGroupRegistry.CreateDefault());

        AgentReport report = await coordinator.AnalyzeAsync("validar runtime GGUF");

        Assert.Equal("ia-offline", report.GroupId);
        Assert.Equal(AgentReportStatus.Partial, report.Status);
        Assert.Contains(report.Findings, finding => finding.Contains("roteada"));
        Assert.NotNull(coordinator.LastReport);
    }

    [Fact]
    public async Task AnalyzeAsyncUsesRegisteredSpecialist()
    {
        var coordinator = new WorkGroupCoordinator(WorkGroupRegistry.CreateDefault());
        coordinator.Register(new FakeAgent("ia-offline"));

        AgentReport report = await coordinator.AnalyzeAsync("validar runtime GGUF");

        Assert.Equal(AgentReportStatus.Complete, report.Status);
        Assert.Equal("evidence", report.Evidence[0].Type);
    }

    private sealed class FakeAgent(string groupId) : IWorkGroupAgent
    {
        public string GroupId { get; } = groupId;

        public Task<AgentReport> AnalyzeAsync(WorkItem item, WorkGroupDefinition group, CancellationToken cancellationToken = default) =>
            Task.FromResult(new AgentReport
            {
                WorkItemId = item.Id,
                GroupId = group.Id,
                Objective = item.Objective,
                Status = AgentReportStatus.Complete,
                Evidence = new[] { new AgentEvidence { Type = "evidence", Reference = "fake", Observation = "ok", Confidence = 1 } },
                Confidence = 1
            });
    }
}
