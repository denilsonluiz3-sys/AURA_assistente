using System;
using System.Threading;
using System.Threading.Tasks;
using AURA.AI;
using Xunit;

namespace AURA.Tests;

public class ToolRegistryTests
{
    private sealed class StubTool : AgentTool
    {
        private readonly string _name;

        public StubTool(string name) => _name = name;

        public override AgentToolDefinition Definition => new AgentToolDefinition
        {
            Name = _name,
            Description = "stub " + _name
        };

        public override Task<string> ExecuteAsync(string argumentsJson, CancellationToken ct = default)
            => Task.FromResult("ok:" + _name);
    }

    [Fact]
    public void Register_And_Resolve_ByName()
    {
        var reg = new ToolRegistry();
        var tool = new StubTool("alpha");
        reg.Register(tool);

        Assert.Equal(1, reg.Count);
        Assert.True(reg.Contains("alpha"));
        Assert.Same(tool, reg.Resolve("alpha"));
        Assert.Null(reg.Resolve("beta"));
    }

    [Fact]
    public void Constructor_FromEnumerable_RegistersAll()
    {
        var reg = new ToolRegistry(new AgentTool[]
        {
            new StubTool("a"),
            new StubTool("b")
        });

        Assert.Equal(2, reg.Count);
        Assert.NotNull(reg.Resolve("a"));
        Assert.NotNull(reg.Resolve("b"));
    }

    [Fact]
    public void Register_DuplicateName_Replaces()
    {
        var reg = new ToolRegistry();
        var first = new StubTool("same");
        var second = new StubTool("same");
        reg.Register(first);
        reg.Register(second);

        Assert.Equal(1, reg.Count);
        Assert.Same(second, reg.Resolve("same"));
    }

    [Fact]
    public void TryRegister_Duplicate_ReturnsFalse()
    {
        var reg = new ToolRegistry();
        Assert.True(reg.TryRegister(new StubTool("x")));
        Assert.False(reg.TryRegister(new StubTool("x")));
        Assert.Equal(1, reg.Count);
    }

    [Fact]
    public void Register_EmptyName_Throws()
    {
        var reg = new ToolRegistry();
        Assert.Throws<ArgumentException>(() => reg.Register(new StubTool("")));
    }

    [Fact]
    public void Definitions_ReturnsAll()
    {
        var reg = new ToolRegistry();
        reg.Register(new StubTool("list_dir"));
        reg.Register(new StubTool("run_shell"));

        var defs = reg.Definitions();
        Assert.Equal(2, defs.Count);
        Assert.Contains(defs, d => d.Name == "list_dir");
        Assert.Contains(defs, d => d.Name == "run_shell");
    }
}
