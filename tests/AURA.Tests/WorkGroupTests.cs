using AURA.Agents.Workgroups;
using Xunit;

namespace AURA.Tests;

public sealed class WorkGroupTests
{
    [Fact]
    public void DefaultRegistryContainsSpecializedGroupsInObserveMode()
    {
        var registry = WorkGroupRegistry.CreateDefault();

        Assert.True(registry.Count >= 9);
        var offline = registry.Resolve("ia-offline");
        Assert.NotNull(offline);
        Assert.Contains("GGUF", offline!.Mission);
        Assert.Contains(WorkGroupStage.Observe, offline.AllowedStages);
        Assert.DoesNotContain(WorkGroupStage.Publish, offline.AllowedStages);
    }

    [Theory]
    [InlineData("validar o runtime GGUF", "ia-offline")]
    [InlineData("revisar permissões do Android", "seguranca")]
    [InlineData("criar lembrete local", "tarefas")]
    [InlineData("executar testes do APK", "qa-ci")]
    public void RegistryRoutesRequestToSpecializedGroup(string request, string expectedGroup)
    {
        var registry = WorkGroupRegistry.CreateDefault();

        Assert.Equal(expectedGroup, registry.ResolveFor(request).Id);
    }

    [Fact]
    public void RegistryRejectsInvalidDefinitions()
    {
        var registry = new WorkGroupRegistry();
        Assert.Throws<ArgumentException>(() => registry.Register(new WorkGroupDefinition()));
    }

    [Fact]
    public async Task ReportStorePersistsEvidenceAtomically()
    {
        string root = Path.Combine(Path.GetTempPath(), "aura-reports-" + Guid.NewGuid().ToString("N"));
        try
        {
            var store = new AgentReportStore(root);
            var report = new AgentReport
            {
                Id = "report-1",
                WorkItemId = "item-1",
                GroupId = "ia-offline",
                Objective = "Analisar runtime local",
                Status = AgentReportStatus.Partial,
                Evidence = new[]
                {
                    new AgentEvidence
                    {
                        Type = "codigo",
                        Reference = "LocalModelStore",
                        Observation = "Modelo GGUF possui SHA-256",
                        Confidence = 0.95
                    }
                }
            };

            await store.SaveAsync(report);
            var loaded = store.Load("report-1");

            Assert.NotNull(loaded);
            Assert.Equal("ia-offline", loaded!.GroupId);
            Assert.Single(loaded.Evidence);
            Assert.Single(store.List());
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }
}
