using AURA.AI;
using Xunit;

namespace AURA.Tests;

public sealed class AgentToolPolicyTests
{
    [Fact]
    public void PolicyFiltersDefinitionsAndRejectsUnlistedTools()
    {
        var policy = new AgentToolPolicy(new[] { "read_file" });
        var definitions = new[]
        {
            new AgentToolDefinition { Name = "read_file" },
            new AgentToolDefinition { Name = "write_file" }
        };

        var filtered = policy.Filter(definitions);

        Assert.True(policy.Allows("READ_FILE"));
        Assert.False(policy.Allows("write_file"));
        Assert.Single(filtered);
        Assert.Equal("read_file", filtered[0].Name);
    }
}
