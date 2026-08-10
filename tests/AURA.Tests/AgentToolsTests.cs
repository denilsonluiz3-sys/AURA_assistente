using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AURA.AI;
using AURA.Abstractions.Execution;
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

            var shell = new ShellAgentTool(root, new ShellExecutor());
            Assert.Equal("run_shell", shell.Definition.Name);
            Assert.Contains("command", shell.Definition.Required);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ShellAgentTool_RunsCommand_ReturnsExitAndOutput()
    {
        string root = CreateTempWorkspace();
        try
        {
            var tool = new ShellAgentTool(root, new ShellExecutor());
            string result = await tool.ExecuteAsync(
                JsonSerializer.Serialize(new { command = "printf 'ola-shell'" }));

            Assert.Contains("exit=0", result);
            Assert.Contains("ola-shell", result);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ShellAgentTool_EmptyCommand_ReturnsErro()
    {
        var tool = new ShellAgentTool(Path.GetTempPath(), new ShellExecutor());
        string result = await tool.ExecuteAsync("{\"command\":\"\"}");
        Assert.Contains("ERRO", result);
        Assert.Contains("vazio", result);
    }

    [Fact]
    public async Task ShellAgentTool_UnavailableExecutor_ReturnsErro()
    {
        var tool = new ShellAgentTool(Path.GetTempPath(), new UnavailableExecutor());
        string result = await tool.ExecuteAsync(
            JsonSerializer.Serialize(new { command = "echo hi" }));
        Assert.Contains("ERRO", result);
        Assert.Contains("shell", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FormatForLlm_IncludesExitCodeStdoutAndStderr()
    {
        var result = new ExecutionResult
        {
            Success = false,
            ExitCode = 7,
            StandardOutput = "out-line",
            StandardError = "err-line"
        };

        string text = ShellAgentTool.FormatForLlm(result);
        Assert.Contains("exit=7", text);
        Assert.Contains("out-line", text);
        Assert.Contains("stderr: err-line", text);
    }

    private sealed class UnavailableExecutor : IToolExecutor
    {
        public string Name => "unavailable";
        public bool IsAvailable() => false;
        public Task<ExecutionResult> ExecuteAsync(ExecutionRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(ExecutionResult.Failed("should not be called"));
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

    private sealed class FakeHandler : HttpMessageHandler
    {
        private readonly string _reply;
        public FakeHandler(string reply) => _reply = reply;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage _, CancellationToken __)
        {
            string body = JsonSerializer.Serialize(new
            {
                choices = new[]
                {
                    new { message = new { role = "assistant", content = _reply } }
                }
            });
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            });
        }
    }

    [Fact]
    public async Task RunAsync_PersistsTurnInMemoryStore()
    {
        string memPath = Path.Combine(Path.GetTempPath(),
            "aura-mem-test-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            var memory = new MemoryStore(new FakeLogger(), memPath);
            var client = new OpenRouterClient(
                new OpenRouterOptions { ApiKey = "sk-test", Model = "test/model" },
                new FakeLogger());

            var session = new AgentSession(client, Array.Empty<AgentTool>(),
                systemPrompt: null, logger: new FakeLogger(), memory: memory);

            string httpReply = "Olá, sou o agente!";
            using var http = new HttpClient(new FakeHandler(httpReply));
            string result = await session.RunAsync("Oi agente", http);

            Assert.Equal(httpReply, result);

            IReadOnlyList<MemoryEntry> entries = memory.Read();
            Assert.Equal(2, entries.Count);
            Assert.Equal("user",      entries[0].Role);
            Assert.Equal("Oi agente", entries[0].Text);
            Assert.Equal("assistant", entries[1].Role);
            Assert.Equal(httpReply,   entries[1].Text);
        }
        finally
        {
            if (File.Exists(memPath)) File.Delete(memPath);
        }
    }

    [Fact]
    public async Task RunAsync_WithoutMemoryStore_DoesNotThrow()
    {
        var client = new OpenRouterClient(
            new OpenRouterOptions { ApiKey = "sk-test", Model = "test/model" },
            new FakeLogger());

        var session = new AgentSession(client, Array.Empty<AgentTool>());

        using var http = new HttpClient(new FakeHandler("resposta ok"));
        string result = await session.RunAsync("pergunta", http);
        Assert.Equal("resposta ok", result);
    }
}
