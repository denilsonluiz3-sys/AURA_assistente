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
using Xunit;

namespace AURA.Tests;

/// <summary>
/// Regressão para o bug "Function call is missing a thought_signature" em
/// modelos Gemini 3.x via OpenRouter: o AgentSession precisa reenviar, sem
/// alterar, os blocos "reasoning_details" que o provedor devolve junto de
/// uma mensagem assistant com tool_calls.
/// </summary>
public class AgentSessionReasoningTests
{
    private sealed class FakeLogger : AURA.Core.Logging.ILogger
    {
        public void Info(string message) { }
        public void Warning(string message) { }
        public void Error(string message) { }
    }

    /// <summary>
    /// Simula o OpenRouter: 1ª chamada devolve tool_calls + reasoning_details
    /// (formato Gemini). 2ª chamada exige que o mesmo reasoning_details volte
    /// intacto na mensagem assistant; se não vier, devolve 400 (reproduzindo
    /// o erro real "missing a thought_signature").
    /// </summary>
    private sealed class GeminiReasoningHandler : HttpMessageHandler
    {
        private readonly string _toolName;
        private readonly string _argumentsJson;
        private readonly string _toolResultExpectedSnippet;
        public int CallCount { get; private set; }
        public JsonElement? LastRequestBody { get; private set; }

        public GeminiReasoningHandler(string toolName, string argumentsJson, string toolResultExpectedSnippet)
        {
            _toolName = toolName;
            _argumentsJson = argumentsJson;
            _toolResultExpectedSnippet = toolResultExpectedSnippet;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken ct)
        {
            CallCount++;
            string bodyText = await request.Content!.ReadAsStringAsync(ct);
            using JsonDocument doc = JsonDocument.Parse(bodyText);
            LastRequestBody = doc.RootElement.Clone();

            if (CallCount == 1)
            {
                // Primeira rodada: modelo pede a ferramenta e devolve
                // reasoning_details (formato Gemini via OpenRouter).
                string reply = JsonSerializer.Serialize(new
                {
                    choices = new object[]
                    {
                        new
                        {
                            message = new
                            {
                                role = "assistant",
                                content = (string?)null,
                                tool_calls = new object[]
                                {
                                    new
                                    {
                                        id = "call_1",
                                        type = "function",
                                        function = new { name = _toolName, arguments = _argumentsJson }
                                    }
                                },
                                reasoning_details = new object[]
                                {
                                    new
                                    {
                                        type = "reasoning.encrypted",
                                        data = "opaque-signature-abc123",
                                        format = "google-gemini-v1",
                                        index = 0
                                    }
                                }
                            }
                        }
                    }
                });
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(reply, Encoding.UTF8, "application/json")
                };
            }

            // Segunda rodada: valida que a mensagem assistant enviada de
            // volta contém o reasoning_details intacto. Sem isso, o
            // OpenRouter real devolveria 400.
            JsonElement messages = LastRequestBody.Value.GetProperty("messages");
            bool foundIntactReasoning = false;
            bool foundToolResult = false;
            foreach (JsonElement m in messages.EnumerateArray())
            {
                if (m.TryGetProperty("role", out JsonElement role) &&
                    role.GetString() == "assistant" &&
                    m.TryGetProperty("reasoning_details", out JsonElement rd) &&
                    rd.ValueKind == JsonValueKind.Array &&
                    rd.GetArrayLength() == 1 &&
                    rd[0].GetProperty("data").GetString() == "opaque-signature-abc123")
                {
                    foundIntactReasoning = true;
                }

                if (m.TryGetProperty("role", out JsonElement toolRole) &&
                    toolRole.GetString() == "tool" &&
                    m.TryGetProperty("content", out JsonElement content) &&
                    content.GetString() != null &&
                    content.GetString()!.Contains(_toolResultExpectedSnippet))
                {
                    foundToolResult = true;
                }
            }

            if (!foundIntactReasoning)
            {
                string err = JsonSerializer.Serialize(new
                {
                    error = new
                    {
                        code = 400,
                        message = "Function call is missing a thought_signature in functionCall parts."
                    }
                });
                return new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent(err, Encoding.UTF8, "application/json")
                };
            }

            Assert.True(foundToolResult, "resultado da ferramenta não encontrado na 2ª requisição");

            string finalReply = JsonSerializer.Serialize(new
            {
                choices = new object[]
                {
                    new { message = new { role = "assistant", content = "ok, concluído" } }
                }
            });
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(finalReply, Encoding.UTF8, "application/json")
            };
        }
    }

    private static string CreateTempWorkspace()
    {
        string dir = Path.Combine(Path.GetTempPath(), "aura-reasoning-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static OpenRouterClient MakeClient(HttpMessageHandler handler, out HttpClient http)
    {
        http = new HttpClient(handler);
        var options = new OpenRouterOptions { ApiKey = "test-key", Model = "google/gemini-3-flash-preview" };
        return new OpenRouterClient(options, new FakeLogger());
    }

    [Fact]
    public async Task RunShell_ToolResult_SecondCallPreservesReasoningDetails()
    {
        string workspace = CreateTempWorkspace();
        try
        {
            var handler = new GeminiReasoningHandler("run_shell", "{\"command\":\"echo oi\"}", "oi");
            OpenRouterClient client = MakeClient(handler, out HttpClient http);
            var tools = new List<AgentTool> { new ShellAgentTool(workspace) };
            var session = new AgentSession(client, tools, logger: new FakeLogger());

            string result = await session.RunAsync("roda echo oi", http);

            Assert.Equal(2, handler.CallCount);
            Assert.Equal("ok, concluído", result);
        }
        finally
        {
            Directory.Delete(workspace, recursive: true);
        }
    }

    [Fact]
    public async Task ListDir_ToolResult_SecondCallPreservesReasoningDetails()
    {
        string workspace = CreateTempWorkspace();
        try
        {
            File.WriteAllText(Path.Combine(workspace, "arquivo.txt"), "conteudo");
            var handler = new GeminiReasoningHandler("list_dir", "{\"path\":\".\"}", "arquivo.txt");
            OpenRouterClient client = MakeClient(handler, out HttpClient http);
            var tools = new List<AgentTool> { new ListDirTool(workspace) };
            var session = new AgentSession(client, tools, logger: new FakeLogger());

            string result = await session.RunAsync("liste os arquivos", http);

            Assert.Equal(2, handler.CallCount);
            Assert.Equal("ok, concluído", result);
        }
        finally
        {
            Directory.Delete(workspace, recursive: true);
        }
    }

    [Fact]
    public async Task WriteFile_ToolResult_SecondCallPreservesReasoningDetails()
    {
        string workspace = CreateTempWorkspace();
        try
        {
            var handler = new GeminiReasoningHandler(
                "write_file", "{\"path\":\"novo.txt\",\"content\":\"ola\"}", "OK");
            OpenRouterClient client = MakeClient(handler, out HttpClient http);
            var tools = new List<AgentTool> { new WriteFileTool(workspace) };
            var session = new AgentSession(client, tools, logger: new FakeLogger());

            string result = await session.RunAsync("crie o arquivo novo.txt", http);

            Assert.Equal(2, handler.CallCount);
            Assert.Equal("ok, concluído", result);
            Assert.True(File.Exists(Path.Combine(workspace, "novo.txt")));
        }
        finally
        {
            Directory.Delete(workspace, recursive: true);
        }
    }

    [Fact]
    public async Task MissingReasoningDetails_ReproducesOriginal400Bug()
    {
        // Prova que, SEM o fix (reenviando reasoning_details), o mesmo
        // handler reproduz o erro 400 original "missing a thought_signature".
        string workspace = CreateTempWorkspace();
        try
        {
            var handler = new GeminiReasoningHandler("run_shell", "{\"command\":\"echo oi\"}", "oi");
            OpenRouterClient client = MakeClient(handler, out HttpClient http);

            var messages = new List<AgentMessage>
            {
                new AgentMessage { Role = "user", Content = "roda echo oi" }
            };
            AgentChatResponse first = await client.ChatToolsAsync(
                messages,
                new List<AgentToolDefinition> { new ShellAgentTool(workspace).Definition },
                http);

            Assert.NotNull(first.ToolCalls);
            Assert.NotNull(first.ReasoningDetails);

            // Monta a 2ª requisição SEM copiar ReasoningDetails de propósito.
            messages.Add(new AgentMessage { Role = "assistant", ToolCalls = first.ToolCalls });
            messages.Add(new AgentMessage { Role = "tool", ToolCallId = first.ToolCalls![0].Id, Content = "oi" });

            AgentChatResponse second = await client.ChatToolsAsync(
                messages,
                new List<AgentToolDefinition> { new ShellAgentTool(workspace).Definition },
                http);

            Assert.Equal(AgentErrorKind.InvalidRequest, second.ErrorKind);
            Assert.Contains("thought_signature", second.Error);
        }
        finally
        {
            Directory.Delete(workspace, recursive: true);
        }
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, AgentErrorKind.InvalidApiKey)]
    [InlineData(HttpStatusCode.PaymentRequired, AgentErrorKind.PaymentRequired)]
    [InlineData(HttpStatusCode.TooManyRequests, AgentErrorKind.RateLimited)]
    [InlineData(HttpStatusCode.InternalServerError, AgentErrorKind.ProviderError)]
    [InlineData(HttpStatusCode.BadRequest, AgentErrorKind.InvalidRequest)]
    public async Task ChatToolsAsync_ClassifiesHttpErrorsByStatusCode(
        HttpStatusCode status, AgentErrorKind expectedKind)
    {
        var handler = new StatusHandler(status);
        using var http = new HttpClient(handler);
        var options = new OpenRouterOptions { ApiKey = "test-key" };
        var client = new OpenRouterClient(options, new FakeLogger());

        AgentChatResponse response = await client.ChatToolsAsync(
            new List<AgentMessage> { new AgentMessage { Role = "user", Content = "oi" } },
            httpClient: http);

        Assert.Equal(expectedKind, response.ErrorKind);
        Assert.False(string.IsNullOrEmpty(response.Error));
    }

    private sealed class StatusHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;
        public StatusHandler(HttpStatusCode status) => _status = status;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken ct)
        {
            return Task.FromResult(new HttpResponseMessage(_status)
            {
                Content = new StringContent("{\"error\":\"simulado\"}", Encoding.UTF8, "application/json")
            });
        }
    }

    [Fact]
    public async Task ChatToolsAsync_TimeoutIsClassifiedSeparately()
    {
        var handler = new TimeoutHandler();
        using var http = new HttpClient(handler) { Timeout = TimeSpan.FromMilliseconds(50) };
        var options = new OpenRouterOptions { ApiKey = "test-key", TimeoutSeconds = 1 };
        var client = new OpenRouterClient(options, new FakeLogger());

        AgentChatResponse response = await client.ChatToolsAsync(
            new List<AgentMessage> { new AgentMessage { Role = "user", Content = "oi" } },
            httpClient: http);

        Assert.Equal(AgentErrorKind.Timeout, response.ErrorKind);
    }

    private sealed class TimeoutHandler : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken ct)
        {
            await Task.Delay(TimeSpan.FromSeconds(5), ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}
