using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AURA.Core.Logging;
using AURA.AI.Providers;

namespace AURA.AI
{
    public sealed class OpenRouterOptions
    {
        public string Provider { get; set; } = "openai";
        public string ApiKey { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = "https://api.openai.com/v1/chat/completions";
        public string Model { get; set; } = "gpt-5-mini";
        public int MaxTokens { get; set; } = 1500;
        public int TimeoutSeconds { get; set; } = 90;
        public string? AppReference { get; set; }

        public string AuthHeaderName { get; set; } = "Authorization";
        public string AuthScheme { get; set; } = "Bearer ";
        public AiApiFormat ApiFormat { get; set; } = AiApiFormat.OpenAICompletions;
    }

    public sealed class OpenRouterClient
    {
        private static readonly string[] BannedTokens = { "anthropic", "claude" };

        private static readonly JsonSerializerOptions SerializeOpts = new()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        private readonly ILogger _logger;

        public OpenRouterOptions Options { get; }

        public OpenRouterClient(OpenRouterOptions options, ILogger? logger = null)
        {
            Options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? new ConsoleLogger();
            GuardAgainstBanned();
        }

        private void GuardAgainstBanned()
        {
            string hay =
                (Options.Provider ?? string.Empty) + " " +
                (Options.Model ?? string.Empty) + " " +
                (Options.BaseUrl ?? string.Empty);
            foreach (string token in BannedTokens)
            {
                if (hay.Contains(token, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        "Provedor/modelo banido no AURA: '" + token + "' não é permitido.");
                }
            }
        }

        public HttpRequestMessage BuildRequest(string question, string? systemPrompt = null)
        {
            if (string.IsNullOrWhiteSpace(question))
                throw new ArgumentException("A pergunta não pode ser vazia.", nameof(question));

            var messages = new List<object>();
            if (!string.IsNullOrWhiteSpace(systemPrompt))
                messages.Add(new { role = "system", content = systemPrompt });

            messages.Add(new { role = "user", content = question });

            var payload = new
            {
                model = Options.Model,
                max_tokens = Options.MaxTokens,
                messages
            };

            string json = JsonSerializer.Serialize(payload);
            var request = new HttpRequestMessage(HttpMethod.Post, Options.BaseUrl);

            if (!string.Equals(Options.Provider, "ollama", StringComparison.OrdinalIgnoreCase))
            {
                string header = string.IsNullOrWhiteSpace(Options.AuthHeaderName)
                    ? "Authorization"
                    : Options.AuthHeaderName;
                string scheme = Options.AuthScheme ?? "Bearer ";
                request.Headers.TryAddWithoutValidation(header, scheme + Options.ApiKey);

                if (Options.AppReference != null)
                {
                    request.Headers.TryAddWithoutValidation("X-Title", "AURA");
                    request.Headers.TryAddWithoutValidation("X-URL", Options.AppReference);
                }
            }

            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            return request;
        }

        public async Task<string> ChatAsync(string question,
            HttpClient? httpClient = null, CancellationToken ct = default, string? systemPrompt = null)
        {
            EnsureValidApiKey();

            HttpClient client = httpClient ?? ResolveClient();
            HttpRequestMessage request = BuildRequest(question, systemPrompt);

            HttpResponseMessage response = await client.SendAsync(request, ct).ConfigureAwait(false);
            string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                string detail = string.IsNullOrWhiteSpace(body) ? "(sem corpo)" : body;
                if (detail.Length > 500)
                    detail = detail.Substring(0, 500);

                _logger.Error("LLM: " + response.StatusCode + " " + detail);
                throw new HttpRequestException(
                    string.Format("Falha na chamada LLM ({0} {1}): {2}",
                        (int)response.StatusCode, response.StatusCode, detail));
            }

            using var document = JsonDocument.Parse(body);
            JsonElement root = document.RootElement;
            if (root.TryGetProperty("choices", out JsonElement choices) &&
                choices.GetArrayLength() > 0)
            {
                JsonElement first = choices[0];
                if (first.TryGetProperty("message", out JsonElement message) &&
                    message.TryGetProperty("content", out JsonElement content))
                {
                    return content.GetString() ?? string.Empty;
                }
            }

            return body;
        }

        public async Task<AgentChatResponse> ChatToolsAsync(
            List<AgentMessage> messages,
            List<AgentToolDefinition>? tools = null,
            HttpClient? httpClient = null,
            CancellationToken ct = default,
            string? systemPrompt = null)
        {
            EnsureValidApiKey();

            // Payload sem JsonNode: evita "The node already has a parent" ao reenviar
            // tool_calls / reasoning_details em rodadas seguintes do AgentSession.
            var messageList = new List<Dictionary<string, object?>>();

            if (!string.IsNullOrWhiteSpace(systemPrompt))
            {
                messageList.Add(new Dictionary<string, object?>
                {
                    ["role"] = "system",
                    ["content"] = systemPrompt
                });
            }

            if (messages != null)
            {
                foreach (AgentMessage m in messages)
                {
                    var mo = new Dictionary<string, object?>
                    {
                        ["role"] = m.Role
                    };

                    if (m.Content != null)
                        mo["content"] = m.Content;

                    if (m.ToolCallId != null)
                        mo["tool_call_id"] = m.ToolCallId;

                    if (m.ToolCalls is { Count: > 0 })
                    {
                        var calls = new List<Dictionary<string, object?>>();
                        foreach (AgentToolCall tc in m.ToolCalls)
                        {
                            calls.Add(new Dictionary<string, object?>
                            {
                                ["id"] = tc.Id,
                                ["type"] = "function",
                                ["function"] = new Dictionary<string, object?>
                                {
                                    ["name"] = tc.Name,
                                    ["arguments"] = tc.ArgumentsJson ?? "{}"
                                }
                            });
                        }

                        mo["tool_calls"] = calls;
                    }

                    // reasoning_details: incluir como JsonElement clonado (valor, não árvore com parent)
                    if (!string.IsNullOrWhiteSpace(m.ReasoningDetailsJson))
                    {
                        try
                        {
                            using var rdDoc = JsonDocument.Parse(m.ReasoningDetailsJson);
                            mo["reasoning_details"] = rdDoc.RootElement.Clone();
                        }
                        catch (JsonException)
                        {
                            // ignora reasoning inválido
                        }
                    }

                    messageList.Add(mo);
                }
            }

            var payload = new Dictionary<string, object?>
            {
                ["model"] = Options.Model,
                ["max_tokens"] = Options.MaxTokens,
                ["messages"] = messageList
            };

            if (tools is { Count: > 0 })
            {
                var toolsArray = new List<Dictionary<string, object?>>();
                foreach (AgentToolDefinition t in tools)
                {
                    var props = new Dictionary<string, object?>();
                    foreach (KeyValuePair<string, AgentToolParameter> p in t.Parameters)
                    {
                        props[p.Key] = new Dictionary<string, object?>
                        {
                            ["type"] = p.Value.Type,
                            ["description"] = p.Value.Description
                        };
                    }

                    var schema = new Dictionary<string, object?>
                    {
                        ["type"] = "object",
                        ["properties"] = props
                    };
                    if (t.Required.Count > 0)
                        schema["required"] = t.Required.ToList();

                    toolsArray.Add(new Dictionary<string, object?>
                    {
                        ["type"] = "function",
                        ["function"] = new Dictionary<string, object?>
                        {
                            ["name"] = t.Name,
                            ["description"] = t.Description,
                            ["parameters"] = schema
                        }
                    });
                }

                payload["tools"] = toolsArray;
            }

            string json = JsonSerializer.Serialize(payload, SerializeOpts);
            HttpClient client = httpClient ?? ResolveClient();
            var request = new HttpRequestMessage(HttpMethod.Post, Options.BaseUrl);

            string header = string.IsNullOrWhiteSpace(Options.AuthHeaderName)
                ? "Authorization"
                : Options.AuthHeaderName;
            string scheme = Options.AuthScheme ?? "Bearer ";
            request.Headers.TryAddWithoutValidation(header, scheme + Options.ApiKey);

            if (Options.AppReference != null)
            {
                request.Headers.TryAddWithoutValidation("X-Title", "AURA");
                request.Headers.TryAddWithoutValidation("X-URL", Options.AppReference);
            }

            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response;
            string body;
            try
            {
                response = await client.SendAsync(request, ct).ConfigureAwait(false);
                body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return new AgentChatResponse
                {
                    Error = "A chamada ao provedor LLM expirou ou foi cancelada.",
                    ErrorKind = AgentErrorKind.Timeout
                };
            }
            catch (HttpRequestException hex)
            {
                return new AgentChatResponse
                {
                    Error = "Falha de rede ao falar com o provedor LLM: " + hex.Message,
                    ErrorKind = AgentErrorKind.Network
                };
            }

            if (!response.IsSuccessStatusCode)
            {
                string detail = string.IsNullOrWhiteSpace(body) ? "(sem corpo)" : body;
                if (detail.Length > 500)
                    detail = detail.Substring(0, 500);

                _logger.Error("LLM: " + response.StatusCode + " " + detail);
                return new AgentChatResponse
                {
                    Error = string.Format("Falha na chamada LLM ({0} {1}): {2}",
                        (int)response.StatusCode, response.StatusCode, detail),
                    ErrorKind = ClassifyError(response.StatusCode)
                };
            }

            try
            {
                using var document = JsonDocument.Parse(body);
                JsonElement root = document.RootElement;
                if (root.TryGetProperty("choices", out JsonElement choices) &&
                    choices.GetArrayLength() > 0)
                {
                    JsonElement message = choices[0];
                    if (message.TryGetProperty("message", out JsonElement msg))
                    {
                        string? content = ReadContentString(msg);
                        string? reasoningJson = ReadReasoningDetailsJson(msg);
                        var calls = new List<AgentToolCall>();
                        if (msg.TryGetProperty("tool_calls", out JsonElement toolCalls))
                        {
                            foreach (JsonElement call in toolCalls.EnumerateArray())
                            {
                                string id = GetProp(call, "id") ?? string.Empty;
                                string name = string.Empty;
                                string argumentsJson = "{}";
                                if (call.TryGetProperty("function", out JsonElement fn))
                                {
                                    name = GetProp(fn, "name") ?? string.Empty;
                                    argumentsJson = GetProp(fn, "arguments") ?? "{}";
                                }

                                calls.Add(new AgentToolCall
                                {
                                    Id = id,
                                    Name = name,
                                    ArgumentsJson = argumentsJson
                                });
                            }
                        }

                        if (calls.Count == 0)
                        {
                            List<AgentToolCall>? textCalls = TryParseTextToolCall(content);
                            if (textCalls is { Count: > 0 })
                            {
                                return new AgentChatResponse
                                {
                                    Content = null,
                                    ToolCalls = textCalls,
                                    ReasoningDetailsJson = reasoningJson
                                };
                            }
                        }

                        return new AgentChatResponse
                        {
                            Content = content,
                            ToolCalls = calls.Count > 0 ? calls : null,
                            ReasoningDetailsJson = reasoningJson
                        };
                    }
                }

                return new AgentChatResponse { Content = body };
            }
            catch (JsonException jex)
            {
                _logger.Error("LLM: resposta inválida: " + jex.Message);
                return new AgentChatResponse { Error = "Resposta inválida do modelo: " + jex.Message };
            }
        }

        /// <summary>Extrai reasoning_details como texto JSON bruto (sem JsonNode).</summary>
        private static string? ReadReasoningDetailsJson(JsonElement msg)
        {
            if (!msg.TryGetProperty("reasoning_details", out JsonElement rd) ||
                rd.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
                return null;

            return rd.GetRawText();
        }

        private static AgentErrorKind ClassifyError(HttpStatusCode status)
        {
            switch (status)
            {
                case HttpStatusCode.BadRequest:
                    return AgentErrorKind.InvalidRequest;
                case HttpStatusCode.Unauthorized:
                case HttpStatusCode.Forbidden:
                    return AgentErrorKind.InvalidApiKey;
                case (HttpStatusCode)402:
                    return AgentErrorKind.PaymentRequired;
                case HttpStatusCode.TooManyRequests:
                    return AgentErrorKind.RateLimited;
                default:
                    return (int)status >= 500
                        ? AgentErrorKind.ProviderError
                        : AgentErrorKind.Unknown;
            }
        }

        private void EnsureValidApiKey()
        {
            if (string.Equals(Options.Provider, "ollama", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(Options.BaseUrl))
                    throw new InvalidOperationException("Endpoint do Ollama não configurado.");
                return;
            }

            if (string.IsNullOrWhiteSpace(Options.ApiKey))
            {
                throw new InvalidOperationException(
                    "ApiKey do provedor LLM não configurada. Defina OpenRouterOptions.ApiKey.");
            }

            if (Options.ApiKey.Length > 200 ||
                Options.ApiKey.IndexOfAny(new[] { ' ', '\t', '\r', '\n' }) >= 0)
            {
                throw new InvalidOperationException(
                    "Chave de API inválida (parece conter conteúdo de log). " +
                    "Toque em 'Restaurar padrão' na aba Correções e digite a chave manualmente na aba Assistente.");
            }
        }

        private HttpClient ResolveClient()
        {
            return new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(Options.TimeoutSeconds > 0 ? Options.TimeoutSeconds : 90)
            };
        }

        private static List<AgentToolCall>? TryParseTextToolCall(string? content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return null;

            string text = content.Trim();

            if (text.StartsWith("```"))
            {
                int firstNewline = text.IndexOf('\n');
                int lastFence = text.LastIndexOf("```");
                if (firstNewline >= 0 && lastFence > firstNewline)
                    text = text.Substring(firstNewline + 1, lastFence - firstNewline - 1).Trim();
            }

            try
            {
                using var doc = JsonDocument.Parse(text);
                JsonElement root = doc.RootElement;

                if (root.ValueKind != JsonValueKind.Object)
                    return null;

                if (!root.TryGetProperty("name", out JsonElement nameEl))
                    return null;

                if (nameEl.ValueKind != JsonValueKind.String)
                    return null;

                string? name = nameEl.GetString();
                if (string.IsNullOrWhiteSpace(name))
                    return null;

                string arguments = "{}";
                if (root.TryGetProperty("arguments", out JsonElement argsEl))
                {
                    arguments = argsEl.GetRawText();
                    if (argsEl.ValueKind == JsonValueKind.String)
                    {
                        string? str = argsEl.GetString();
                        if (!string.IsNullOrWhiteSpace(str))
                            arguments = str;
                    }
                }

                return new List<AgentToolCall>
                {
                    new AgentToolCall
                    {
                        Id = "ollama-tool-" + Guid.NewGuid().ToString("N"),
                        Name = name,
                        ArgumentsJson = arguments
                    }
                };
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static string? GetProp(JsonElement el, string name)
        {
            if (el.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.String)
                return value.GetString();
            return null;
        }

        private static string? ReadContentString(JsonElement message)
        {
            if (!message.TryGetProperty("content", out JsonElement content))
                return null;

            if (content.ValueKind == JsonValueKind.String)
                return content.GetString();

            if (content.ValueKind == JsonValueKind.Array)
            {
                var sb = new StringBuilder();
                foreach (JsonElement part in content.EnumerateArray())
                {
                    if (part.TryGetProperty("text", out JsonElement text) &&
                        text.ValueKind == JsonValueKind.String)
                        sb.Append(text.GetString());
                }

                return sb.Length > 0 ? sb.ToString() : null;
            }

            return null;
        }
    }
}
