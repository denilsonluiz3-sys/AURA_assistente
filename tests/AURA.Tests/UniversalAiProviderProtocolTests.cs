using System.Net;
using System.Net.Http;
using System.Linq;
using System.Text;
using System.Text.Json;
using AURA.AI;
using AURA.AI.UniversalAI;
using Xunit;

namespace AURA.Tests;

/// <summary>Regressões do protocolo de tools nos formatos universais suportados.</summary>
public sealed class UniversalAiProviderProtocolTests
{
    private sealed class EchoTool : AgentTool
    {
        public override AgentToolDefinition Definition
        {
            get
            {
                var definition = new AgentToolDefinition
                {
                    Name = "echo_value",
                    Description = "Retorna o valor recebido."
                };
                definition.Parameters["value"] = new AgentToolParameter { Type = "string", Description = "Valor a retornar." };
                definition.Required.Add("value");
                return definition;
            }
        }

        public override Task<string> ExecuteAsync(string argumentsJson, CancellationToken ct = default)
            => Task.FromResult("resultado da ferramenta");
    }

    private sealed class FakeLogger : AURA.Core.Logging.ILogger
    {
        public void Info(string message) { }
        public void Warning(string message) { }
        public void Error(string message) { }
    }

    private static UniversalAiClient CreateClient(UniversalApiFormat format)
        => new(new UniversalAiClientOptions
        {
            Provider = "test",
            ApiKey = "test-key",
            Model = "test-model",
            BaseUrl = "https://test.invalid/chat",
            ApiFormat = format,
            MaxTokens = 100,
            TimeoutSeconds = 5
        });

    [Fact]
    public async Task Anthropic_ToolUseAndToolResult_RoundTripThroughAgentSession()
    {
        AgentSession.ClearSharedHistory();
        using var http = new HttpClient(new AnthropicHandler());
        var session = new AgentSession(CreateClient(UniversalApiFormat.AnthropicMessages), new AgentTool[] { new EchoTool() }, logger: new FakeLogger());

        string answer = await session.RunAsync("use a ferramenta", http);

        Assert.Equal("anthropic concluído", answer);
    }

    [Fact]
    public async Task Gemini_FunctionCallAndFunctionResponse_RoundTripThroughAgentSession()
    {
        AgentSession.ClearSharedHistory();
        using var http = new HttpClient(new GeminiHandler());
        var session = new AgentSession(CreateClient(UniversalApiFormat.Gemini), new AgentTool[] { new EchoTool() }, logger: new FakeLogger());

        string answer = await session.RunAsync("use a ferramenta", http);

        Assert.Equal("gemini concluído", answer);
    }

    [Fact]
    public async Task OpenAi_ReasoningDetails_ArePreservedInNextToolRound()
    {
        AgentSession.ClearSharedHistory();
        using var http = new HttpClient(new OpenAiReasoningHandler());
        var session = new AgentSession(CreateClient(UniversalApiFormat.OpenAiCompatible), new AgentTool[] { new EchoTool() }, logger: new FakeLogger());

        string answer = await session.RunAsync("use a ferramenta", http);

        Assert.Equal("openai concluído", answer);
    }

    private abstract class ProtocolHandler : HttpMessageHandler
    {
        protected static JsonDocument Read(HttpRequestMessage request)
            => JsonDocument.Parse(request.Content!.ReadAsStringAsync().GetAwaiter().GetResult());

        protected static HttpResponseMessage Json(string body)
            => new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
    }

    private sealed class AnthropicHandler : ProtocolHandler
    {
        private int _calls;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            using var document = Read(request);
            var root = document.RootElement;
            Assert.Equal("2023-06-01", request.Headers.GetValues("anthropic-version").Single());
            Assert.Equal("test-model", root.GetProperty("model").GetString());
            Assert.True(root.GetProperty("tools").GetArrayLength() == 1);

            if (++_calls == 1)
            {
                Assert.Equal("user", root.GetProperty("messages")[0].GetProperty("role").GetString());
                return Task.FromResult(Json("{\"content\":[{\"type\":\"tool_use\",\"id\":\"anthropic-call\",\"name\":\"echo_value\",\"input\":{\"value\":\"x\"}}]}"));
            }

            var messages = root.GetProperty("messages");
            var assistantBlocks = messages[1].GetProperty("content");
            Assert.Equal("tool_use", assistantBlocks[0].GetProperty("type").GetString());
            Assert.Equal("anthropic-call", assistantBlocks[0].GetProperty("id").GetString());
            var result = messages[2].GetProperty("content")[0];
            Assert.Equal("tool_result", result.GetProperty("type").GetString());
            Assert.Equal("anthropic-call", result.GetProperty("tool_use_id").GetString());
            Assert.Contains("resultado da ferramenta", result.GetProperty("content").GetString());
            return Task.FromResult(Json("{\"content\":[{\"type\":\"text\",\"text\":\"anthropic concluído\"}]}"));
        }
    }

    private sealed class GeminiHandler : ProtocolHandler
    {
        private int _calls;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            using var document = Read(request);
            var root = document.RootElement;
            Assert.True(root.GetProperty("tools")[0].GetProperty("functionDeclarations").GetArrayLength() == 1);

            if (++_calls == 1)
            {
                var firstPart = root.GetProperty("contents")[0].GetProperty("parts")[0];
                Assert.True(firstPart.TryGetProperty("text", out _));
                return Task.FromResult(Json("{\"candidates\":[{\"content\":{\"parts\":[{\"functionCall\":{\"name\":\"echo_value\",\"args\":{\"value\":\"x\"}}}]}}]}"));
            }

            var contents = root.GetProperty("contents");
            bool hasCall = false;
            bool hasResult = false;
            foreach (var content in contents.EnumerateArray())
            {
                foreach (var part in content.GetProperty("parts").EnumerateArray())
                {
                    hasCall |= part.TryGetProperty("functionCall", out _);
                    if (part.TryGetProperty("functionResponse", out var response))
                        hasResult |= response.GetProperty("name").GetString() == "echo_value" && response.GetProperty("response").GetProperty("content").GetString()?.Contains("resultado da ferramenta") == true;
                }
            }
            Assert.True(hasCall);
            Assert.True(hasResult);
            return Task.FromResult(Json("{\"candidates\":[{\"content\":{\"parts\":[{\"text\":\"gemini concluído\"}]}}]}"));
        }
    }

    private sealed class OpenAiReasoningHandler : ProtocolHandler
    {
        private int _calls;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            using var document = Read(request);
            var root = document.RootElement;
            if (++_calls == 1)
            {
                return Task.FromResult(Json("{\"choices\":[{\"message\":{\"role\":\"assistant\",\"content\":null,\"reasoning_details\":[{\"type\":\"reasoning.text\",\"text\":\"decisão\"}],\"tool_calls\":[{\"id\":\"reasoning-call\",\"type\":\"function\",\"function\":{\"name\":\"echo_value\",\"arguments\":\"{\\\"value\\\":\\\"x\\\"}\"}}]}}]}"));
            }

            // The session history can contain previous assistant turns; this
            // assertion targets the assistant tool-call turn from this request.
            var assistant = root.GetProperty("messages").EnumerateArray()
                .Last(message => message.GetProperty("role").GetString() == "assistant");
            Assert.True(assistant.TryGetProperty("reasoning_details", out var details));
            Assert.Equal(JsonValueKind.Array, details.ValueKind);
            Assert.Equal("reasoning.text", details[0].GetProperty("type").GetString());
            return Task.FromResult(Json("{\"choices\":[{\"message\":{\"role\":\"assistant\",\"content\":\"openai concluído\"}}]}"));
        }
    }
}
