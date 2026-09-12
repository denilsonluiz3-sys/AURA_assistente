using AURA.Agents.Workgroups;
using Xunit;

namespace AURA.Tests;

public sealed class WorkGroupPolicyIntegrationTests
{
    [Theory]
    [InlineData("ler arquivos do workspace")]
    [InlineData("validar o runtime GGUF")]
    public void ObservationPolicyIsCreatedByCoordinator(string objective)
    {
        var coordinator = new WorkGroupCoordinator(WorkGroupRegistry.CreateDefault());

        var policy = coordinator.CreateObservationPolicy(objective);

        Assert.True(policy.Allows("read_file"));
        Assert.True(policy.Allows("list_dir"));
        Assert.False(policy.Allows("write_file"));
        Assert.False(policy.Allows("shell"));
    }

    [Fact]
    public void ObservationPolicyRejectsBlankObjective()
    {
        var coordinator = new WorkGroupCoordinator(WorkGroupRegistry.CreateDefault());

        Assert.Throws<ArgumentException>(() => coordinator.CreateObservationPolicy(" "));
    }
}
