using AURA.Agents.Workgroups;
using Xunit;

namespace AURA.Tests;

public sealed class WorkGroupPolicyTests
{
    [Fact]
    public void CoordinatorBuildsToolPolicyFromGroupAndStage()
    {
        var coordinator = new WorkGroupCoordinator(WorkGroupRegistry.CreateDefault());
        var item = new WorkItem { GroupId = "ia-offline", Stage = WorkGroupStage.Observe };

        var policy = coordinator.CreateToolPolicy(item);

        Assert.True(policy.Allows("read_file"));
        Assert.False(policy.Allows("shell"));
    }

    [Fact]
    public void CoordinatorRejectsDisallowedStage()
    {
        var coordinator = new WorkGroupCoordinator(WorkGroupRegistry.CreateDefault());
        var item = new WorkItem { GroupId = "ia-offline", Stage = WorkGroupStage.ExecuteControlled };

        Assert.Throws<InvalidOperationException>(() => coordinator.CreateToolPolicy(item));
    }

    [Fact]
    public void RegistryRejectsDuplicateGroupIds()
    {
        var registry = new WorkGroupRegistry();
        registry.Register(new WorkGroupDefinition { Id = "test", Name = "Teste" });

        Assert.Throws<InvalidOperationException>(() => registry.Register(new WorkGroupDefinition { Id = "test", Name = "Outro" }));
    }
}
