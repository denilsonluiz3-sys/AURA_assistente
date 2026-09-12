using AURA.Agents.Workgroups;
using Xunit;

namespace AURA.Tests;

public sealed class QaCiPlanningAgentTests
{
    [Fact]
    public async Task DefaultCoordinatorUsesQaSpecialistWithoutClaimingValidation()
    {
        var coordinator = new WorkGroupCoordinator(WorkGroupRegistry.CreateDefault());

        AgentReport report = await coordinator.AnalyzeAsync("executar testes do APK Android");

        Assert.Equal("qa-ci", report.GroupId);
        Assert.Equal(AgentReportStatus.Complete, report.Status);
        Assert.Contains(report.Findings, item => item.Contains("Nenhum build"));
        Assert.Contains(report.Evidence, item => item.Reference == "android-apk");
    }
}
