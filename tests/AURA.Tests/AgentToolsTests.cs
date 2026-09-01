using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AURA.AI;
using AURA.AI.UniversalAI;
using AURA.Core.Logging;
using AURA.Memory;
using AURA.Modules.Executors;
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
            string result = await writer.ExecuteAsync(JsonSerializer.Serialize(new { path = "sub/notas.md", content = "linha 1\nlinha 2" }));
            Assert.Contains("OK", result);
            Assert.True(File.Exists(Path.Combine(root, "sub", "notas.md")));
            var reader = new ReadFileTool(root);
            string content = await reader.ExecuteAsync(JsonSerializer.Serialize(new { path = "sub/notas.md" }));
            Assert.Equal("linha 1\nlinha 2", content);
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    [Fact]
    public async Task EditFileReplacesFirstOccurrence()
    {
        string root = CreateTempWorkspace();
        try
        {
            var writer = new WriteFileTool(root);
            await writer.ExecuteAsync(JsonSerializer.Serialize(new { path = "a.txt", content = "aaa BBB aaa" }));
            var editor = new EditFileTool(root);
            string result = await editor.ExecuteAsync(JsonSerializer.Serialize(new { path = "a.txt", old_text = "aaa", new_text = "xxx" }));
            Assert.Contains("OK", result);
            Assert.Equal("xxx BBB aaa", await File.ReadAllTextAsync(Path.Combine(root, "a.txt")));
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    [Fact]
    public async Task EditFileReportsMissingText()
    {
        string root = CreateTempWorkspace();
        try
        {
            var writer = new WriteFileTool(root);
            await writer.ExecuteAsync(JsonSerializer.Serialize(new { path = "a.txt", content = "conteudo" }));
            var editor = new EditFileTool(root);
            string result = await editor.ExecuteAsync(JsonSerializer.Serialize(new { path = "a.txt", old_text = "nao existe", new_text = "x" }));
            Assert.Contains("ERRO", result);
        }
        finally { Directory.Delete(root, recursive: true); }
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
        finally { Directory.Delete(root, recursive: true); }
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
            await Assert.ThrowsAsync<InvalidOperationException>(() => writer.ExecuteAsync(JsonSerializer.Serialize(new { path = rawPath, content = "x" })));
        }
        finally { Directory.Delete(root, recursive: true); }
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
            var shell = new ShellAgentTool(root, new ShellExecutor());
            Assert.Contains("command", shell.Definition.Required);
        }
        finally { Directory.Delete(root, recursive: true); }
    }
}

public class AgentSessionMemoryTests
{
    private sealed class FakeLogger : ILogger
    {
        public void Info(string m) { }
        public void Warning(string m) { }
        public void Error(string m) { }
    }

    private sealed class FakeUniversalClient : IUniversalAiClient
    {
        private readonly string _reply;
        public FakeUniversalClient(string reply) => _reply = reply;
        public UniversalAiClientOptions Options { get; } = new() { Provider = "test", Model = "test/model", BaseUrl = "https://test.invalid/chat" };
        public Task<string> ChatAsync(string question, HttpClient? httpClient = null, string? systemPrompt = null, CancellationToken ct = default) => Task.FromResult(_reply);
        public Task<AgentChatResponse> ChatToolsAsync(IReadOnlyList<AgentMessage> messages, IReadOnlyList<AgentToolDefinition> tools, HttpClient? httpClient = null, CancellationToken ct = default, string? systemPrompt = null)
            => Task.FromResult(new AgentChatResponse { Content = _reply });
    }

    [Fact]
    public async Task RunAsync_PersistsTurnInMemoryStore()
    {
        string memPath = Path.Combine(Path.GetTempPath(), "aura-mem-test-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            var memory = new MemoryStore(new FakeLogger(), memPath);
            string reply = "Olá, sou o agente!";
            var client = new FakeUniversalClient(reply);
            var session = new AgentSession(client, Array.Empty<AgentTool>(), systemPrompt: null, logger: new FakeLogger(), memory: memory);
            string result = await session.RunAsync("Oi agente");
            Assert.Equal(reply, result);
            IReadOnlyList<MemoryEntry> entries = memory.Read();
            Assert.Equal(2, entries.Count);
            Assert.Equal("user", entries[0].Role);
            Assert.Equal("Oi agente", entries[0].Text);
            Assert.Equal("assistant", entries[1].Role);
            Assert.Equal(reply, entries[1].Text);
        }
        finally { if (File.Exists(memPath)) File.Delete(memPath); }
    }

    [Fact]
    public async Task RunAsync_WithoutMemoryStore_DoesNotThrow()
    {
        var session = new AgentSession(new FakeUniversalClient("resposta ok"), Array.Empty<AgentTool>());
        string result = await session.RunAsync("pergunta");
        Assert.Equal("resposta ok", result);
    }
}
