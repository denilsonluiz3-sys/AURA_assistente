using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using AURA.AI;
using Xunit;

namespace AURA.Tests;

public class AgentToolsTests
{
    private static string CreateTempWorkspace()
    {
        string dir = Path.Combine(Path.GetTempPath(), "aura-agent-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    [Fact]
    public async Task WriteAndReadRoundTrip()
    {
        string root = CreateTempWorkspace();
        try
        {
            var writer = new WriteFileTool(root);
            string result = await writer.ExecuteAsync(
                JsonSerializer.Serialize(new { path = "sub/notas.md", content = "linha 1\nlinha 2" }));

            Assert.Contains("OK", result);
            Assert.True(File.Exists(Path.Combine(root, "sub", "notas.md")));

            var reader = new ReadFileTool(root);
            string content = await reader.ExecuteAsync(
                JsonSerializer.Serialize(new { path = "sub/notas.md" }));

            Assert.Equal("linha 1\nlinha 2", content);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task EditFileReplacesFirstOccurrence()
    {
        string root = CreateTempWorkspace();
        try
        {
            var writer = new WriteFileTool(root);
            await writer.ExecuteAsync(
                JsonSerializer.Serialize(new { path = "a.txt", content = "aaa BBB aaa" }));

            var editor = new EditFileTool(root);
            string result = await editor.ExecuteAsync(JsonSerializer.Serialize(new
            {
                path = "a.txt",
                old_text = "aaa",
                new_text = "xxx"
            }));

            Assert.Contains("OK", result);
            string final = await File.ReadAllTextAsync(Path.Combine(root, "a.txt"));
            Assert.Equal("xxx BBB aaa", final);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task EditFileReportsMissingText()
    {
        string root = CreateTempWorkspace();
        try
        {
            var writer = new WriteFileTool(root);
            await writer.ExecuteAsync(
                JsonSerializer.Serialize(new { path = "a.txt", content = "conteudo" }));

            var editor = new EditFileTool(root);
            string result = await editor.ExecuteAsync(JsonSerializer.Serialize(new
            {
                path = "a.txt",
                old_text = "nao existe",
                new_text = "x"
            }));

            Assert.Contains("ERRO", result);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ListDirShowsEntries()
    {
        string root = CreateTempWorkspace();
        try
        {
            File.WriteAllText(Path.Combine(root, "doc.md"), "oi");
            Directory.CreateDirectory(Path.Combine(root, "src"));

            var lister = new ListDirTool(root);
            string result = await lister.ExecuteAsync("{}");

            Assert.Contains("doc.md", result);
            Assert.Contains("src/", result);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Theory]
    [InlineData("../../fora.txt")]
    [InlineData("/etc/passwd")]
    [InlineData("..\\..\\escapou.txt")]
    public async Task PathTraversalIsBlocked(string rawPath)
    {
        string root = CreateTempWorkspace();
        try
        {
            var writer = new WriteFileTool(root);
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                writer.ExecuteAsync(JsonSerializer.Serialize(new { path = rawPath, content = "x" })));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ToolDefinitionsExposeRequiredParameters()
    {
        string root = CreateTempWorkspace();
        try
        {
            var write = new WriteFileTool(root);
            Assert.Equal("write_file", write.Definition.Name);
            Assert.Contains("path", write.Definition.Required);
            Assert.Contains("content", write.Definition.Required);

            var shell = new ShellAgentTool(root);
            Assert.Contains("command", shell.Definition.Required);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
