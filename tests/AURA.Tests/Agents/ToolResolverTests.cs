using AURA.Agents;

namespace AURA.Tests.Agents;

public sealed class ToolResolverTests
{
    [Fact]
    public async Task Resolve_ShouldReturnRegisteredTool()
    {
        var tool = new DelegateTool("search", (_, _, _) => Task.FromResult(new ToolResult(true, "ok")));
        var resolver = new ToolResolver(new[] { tool });

        ITool resolved = resolver.Resolve("SEARCH");
        ToolResult result = await resolved.ExecuteAsync("x", new());

        Assert.Same(tool, resolved);
        Assert.True(result.Success);
    }

    [Fact]
    public async Task Resolve_ShouldUseConversationFallback()
    {
        var tool = new DelegateTool("conversar", (_, _, _) => Task.FromResult(new ToolResult(true, "fallback")));
        var resolver = new ToolResolver(new[] { tool });

        ToolResult result = await resolver.Resolve("unknown").ExecuteAsync("x", new());

        Assert.True(result.Success);
        Assert.Equal("fallback", result.Output);
    }

    [Fact]
    public async Task SearchTool_ShouldUseQueryParameter()
    {
        string? received = null;
        var tool = new SearchTool((query, _) =>
        {
            received = query;
            return Task.FromResult("resultado");
        });

        ToolResult result = await tool.ExecuteAsync("ignorado", new() { ["query"] = "aura local" });

        Assert.True(result.Success);
        Assert.Equal("aura local", received);
    }
}
