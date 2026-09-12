using AURA.Agents.Workgroups;
using Xunit;

namespace AURA.Tests;

public sealed class OfflineAiPlanningAgentTests
{
    [Fact]
    public async Task DefaultCoordinatorUsesOfflineSpecialistSafely()
    {
        var coordinator = new WorkGroupCoordinator(WorkGroupRegistry.CreateDefault());

        AgentReport report = await coordinator.AnalyzeAsync("validar runtime GGUF");

        Assert.Equal("ia-offline", report.GroupId);
        Assert.Equal(AgentReportStatus.Complete, report.Status);
        Assert.Contains(report.Evidence, item => item.Reference == "model-import");
        Assert.Contains(report.Evidence, item => item.Reference == "tool-loop");
        Assert.Contains("modelo GGUF", report.NextStep);
    }
}
