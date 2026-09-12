using AURA.Agents.Workgroups;
using Xunit;

namespace AURA.Tests;

public sealed class WorkItemStoreTests
{
    [Fact]
    public async Task CoordinatorPersistsCompletedWorkItem()
    {
        string root = Path.Combine(Path.GetTempPath(), "aura-items-" + Guid.NewGuid().ToString("N"));
        try
        {
            var items = new WorkItemStore(root);
            var coordinator = new WorkGroupCoordinator(WorkGroupRegistry.CreateDefault(), items: items);

            AgentReport report = await coordinator.AnalyzeAsync("validar runtime GGUF");
            WorkItem? item = items.Load(report.WorkItemId);

            Assert.NotNull(item);
            Assert.Equal(WorkItemStatus.Completed, item!.Status);
            Assert.Equal("ia-offline", item.GroupId);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }
}
