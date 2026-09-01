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
using AURA.Modules.Executors;
using Xunit;

namespace AURA.Tests;

/// <summary>Regressões do fluxo AgentSession → contrato universal → HTTP.</summary>
public class AgentSessionUniversalClientTests
{
    private sealed class FakeLogger : AURA.Core.Logging.ILogger
    {
        public void Info(string message) { }
        public void Warning(string message) { }
        public void Error(string message) { }
    }

    private sealed class ToolRoundTripHandler : HttpMessageHandler
    {
        private readonly string _toolName;
        private readonly string _argumentsJson;
        private readonly string _expectedToolResult;
        public int CallCount { get; private set; }

        public ToolRoundTripHandler(string toolName, string argumentsJson, string expectedToolResult)
        {
            _toolName = toolName;
            _argumentsJson = argumentsJson;
            _expectedToolResult = expectedToolResult;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            CallCount++;
            string body = await request.Content!.ReadAsStringAsync(ct);
            using JsonDocument doc = JsonDocument.Parse(body);

            if (CallCount == 1)
            {
                string reply = JsonSerializer.Serialize(new
                {
                    choices = new[]
                    {
                        new
                        {
                            message = new
                            {
                                role = "assistant",
                                content = (string?)null,
                                tool_calls = new[]
                                {
                                    new
                                    {
                                        id = "call_1",
                                        type = "function",
                                        function = new { name = _toolName, arguments = _argumentsJson }
                                    }
                                }
                            }
                        }
                    }
                });
                return Ok(reply);
            }

            bool foundToolResult = false;
            foreach (JsonElement message in doc.RootElement.GetProperty("messages").EnumerateArray())
            {
                if (message.TryGetProperty("role", out JsonElement role) &&
                    role.GetString() == "tool" &&
                    message.TryGetProperty("content", out JsonElement content) &&
                    content.GetString()?.Contains(_expectedToolResult, StringComparison.Ordinal) == true)
                {
                    foundToolResult = true;
                }
            }

            Assert.True(foundToolResult, "O resultado da ferramenta não foi reenviado pelo cliente universal.");
            return Ok(JsonSerializer.Serialize(new
            {
                choices = new[] { new { message = new { role = "assistant", content = "ok, concluído" } } }
            }));
        }

        private static HttpResponseMessage Ok(string body) => new(HttpStatusCode.OK)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
    }

    private static IUniversalAiClient CreateClient()
        => new UniversalAiClient(new UniversalAiClientOptions
        {
            Provider = "test",
            ApiKey = "test-key",
            Model = "test/model",
            BaseUrl = "https://test.invalid/chat",
            ApiFormat = UniversalApiFormat.OpenAiCompatible,
            MaxTokens = 100,
            TimeoutSeconds = 5
        });

    private static string CreateTempWorkspace()
    {
        string dir = Path.Combine(Path.GetTempPath(), "aura-universal-agent-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    [Fact]
    public async Task RunShell_ToolResult_IsReturnedThroughUniversalClient()
    {
        string workspace = CreateTempWorkspace();
        try
        {
            using var http = new HttpClient(new ToolRoundTripHandler("run_shell", "{\"command\":\"echo oi\"}", "oi"));
            var tools = new List<AgentTool> { new ShellAgentTool(workspace, new ShellExecutor()) };
            var session = new AgentSession(CreateClient(), tools, logger: new FakeLogger());
            string result = await session.RunAsync("roda echo oi", http);
            Assert.Equal("ok, concluído", result);
        }
        finally { Directory.Delete(workspace, recursive: true); }
    }

    [Fact]
    public async Task ListDir_ToolResult_IsReturnedThroughUniversalClient()
    {
        string workspace = CreateTempWorkspace();
        try
        {
            File.WriteAllText(Path.Combine(workspace, "arquivo.txt"), "conteudo");
            using var http = new HttpClient(new ToolRoundTripHandler("list_dir", "{\"path\":\".\"}", "arquivo.txt"));
            var session = new AgentSession(CreateClient(), new List<AgentTool> { new ListDirTool(workspace) }, logger: new FakeLogger());
            string result = await session.RunAsync("liste os arquivos", http);
            Assert.Equal("ok, concluído", result);
        }
        finally { Directory.Delete(workspace, recursive: true); }
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, AgentErrorKind.InvalidApiKey)]
    [InlineData(HttpStatusCode.PaymentRequired, AgentErrorKind.PaymentRequired)]
    [InlineData(HttpStatusCode.TooManyRequests, AgentErrorKind.RateLimited)]
    [InlineData(HttpStatusCode.InternalServerError, AgentErrorKind.ProviderError)]
    [InlineData(HttpStatusCode.BadRequest, AgentErrorKind.InvalidRequest)]
    public async Task ChatToolsAsync_ClassifiesHttpErrors(HttpStatusCode status, AgentErrorKind expectedKind)
    {
        using var http = new HttpClient(new StatusHandler(status));
        AgentChatResponse response = await CreateClient().ChatToolsAsync(
            new List<AgentMessage> { new() { Role = "user", Content = "oi" } },
            Array.Empty<AgentToolDefinition>(), http);
        Assert.Equal(expectedKind, response.ErrorKind);
        Assert.False(string.IsNullOrWhiteSpace(response.Error));
    }

    private sealed class StatusHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;
        public StatusHandler(HttpStatusCode status) => _status = status;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            => Task.FromResult(new HttpResponseMessage(_status)
            {
                Content = new StringContent("{\"error\":\"simulado\"}", Encoding.UTF8, "application/json")
            });
    }

    [Fact]
    public async Task ChatToolsAsync_TimeoutIsClassifiedSeparately()
    {
        using var http = new HttpClient(new TimeoutHandler()) { Timeout = TimeSpan.FromMilliseconds(50) };
        AgentChatResponse response = await CreateClient().ChatToolsAsync(
            new List<AgentMessage> { new() { Role = "user", Content = "oi" } },
            Array.Empty<AgentToolDefinition>(), http);
        Assert.Equal(AgentErrorKind.Timeout, response.ErrorKind);
    }

    private sealed class TimeoutHandler : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            await Task.Delay(TimeSpan.FromSeconds(5), ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}
