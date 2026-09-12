using AURA.AI.UniversalAI;
using Xunit;

namespace AURA.Tests;

public sealed class LocalToolCallParserTests
{
    [Fact]
    public void ParsesStrictLocalToolCall()
    {
        bool parsed = LocalToolCallParser.TryParse(
            "{\"type\":\"tool_call\",\"name\":\"create_task\",\"arguments\":{\"title\":\"Revisar arquivo\"}}",
            out var call,
            out var error);

        Assert.True(parsed, error);
        Assert.NotNull(call);
        Assert.Equal("create_task", call!.Name);
        Assert.Equal("{\"title\":\"Revisar arquivo\"}", call.ArgumentsJson);
    }

    [Fact]
    public void ParsesOpenAiCompatibleFunctionShape()
    {
        bool parsed = LocalToolCallParser.TryParse(
            "```json\n{\"id\":\"call-1\",\"function\":{\"name\":\"list_tasks\",\"arguments\":\"{\\\"status\\\":\\\"pending\\\"}\"}}\n```",
            out var call,
            out var error);

        Assert.True(parsed, error);
        Assert.NotNull(call);
        Assert.Equal("call-1", call!.Id);
        Assert.Equal("list_tasks", call.Name);
        Assert.Contains("pending", call.ArgumentsJson);
    }

    [Fact]
    public void ParsesMultipleCalls()
    {
        bool parsed = LocalToolCallParser.TryParseMany(
            "{\"tool_calls\":[{\"name\":\"one\",\"arguments\":{}},{\"name\":\"two\",\"arguments\":{\"value\":1}}]}",
            out var calls,
            out var error);

        Assert.True(parsed, error);
        Assert.Equal(2, calls.Count);
        Assert.Equal("one", calls[0].Name);
        Assert.Equal("two", calls[1].Name);
    }

    [Fact]
    public void RejectsFreeTextAndInvalidJson()
    {
        Assert.False(LocalToolCallParser.TryParse("faça isso agora", out _, out _));
        Assert.False(LocalToolCallParser.TryParse("{invalido}", out _, out _));
    }
}
