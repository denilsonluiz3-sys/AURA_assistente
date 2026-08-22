using AURA.Agents;
using Xunit;

namespace AURA.Tests.Agents;

public sealed class FileToolTests
{
    [Fact]
    public async Task WriteThenRead_ShouldStayInsideWorkspace()
    {
        string workspace = Path.Combine(Path.GetTempPath(), "aura-tests-" + Guid.NewGuid().ToString("N"));
        try
        {
            var tool = new FileTool(workspace);

            ToolResult write = await tool.ExecuteAsync("write", new()
            {
                ["operation"] = "write",
                ["path"] = "hello.txt",
                ["content"] = "AURA"
            });

            ToolResult read = await tool.ExecuteAsync("read", new()
            {
                ["operation"] = "read",
                ["path"] = "hello.txt"
            });

            Assert.True(write.Success);
            Assert.True(read.Success);
            Assert.Equal("AURA", read.Output);
        }
        finally
        {
            if (Directory.Exists(workspace)) Directory.Delete(workspace, true);
        }
    }

    [Fact]
    public async Task PathTraversal_ShouldBeRejected()
    {
        string workspace = Path.Combine(Path.GetTempPath(), "aura-tests-" + Guid.NewGuid().ToString("N"));
        try
        {
            var tool = new FileTool(workspace);
            ToolResult result = await tool.ExecuteAsync("write", new()
            {
                ["operation"] = "write",
                ["path"] = "../outside.txt",
                ["content"] = "blocked"
            });

            Assert.False(result.Success);
        }
        finally
        {
            if (Directory.Exists(workspace)) Directory.Delete(workspace, true);
        }
    }
}
