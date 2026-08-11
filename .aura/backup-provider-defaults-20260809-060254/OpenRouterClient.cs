using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using AURA.Core.Logging;
using AURA.Memory;

namespace AURA.AI
{
    /// <summary>
    /// Configurações do provedor LLM. O mobile (AURA.AI) expõe o mesmo
    /// provedor via MemoryService; aqui o cliente HTTP direto. Defaults seguem
    /// o config do aichat (OpenRouter, qwen/qwen-plus).
    /// </summary>
    public sealed class OpenRouterOptions
    {
        public string Provider { get; set; } = "openrouter";
        public string ApiKey { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = "http://127.0.0.1:11434/v1/chat/completions";
        public string Model { get; set; } = "qwen2.5-coder:1.5b";
        public int MaxTokens { get; set; } = 1500;
        public int TimeoutSeconds { get; set; } = 90;
        public string? AppReference { get; set; }
    }

    /// <summary>
    /// Cliente mínimo para OpenRouter chat completions. Construa a requisição
    /// (testável sem rede) com BuildRequest; execute com ChatAsync.
    /// </summary>
    public sealed class OpenRouterClient
    {
        private readonly ILogger _logger;

        public OpenRouterOptions Options { get; }

        public OpenRouterClient(OpenRouterOptions options, ILogger? logger = null)
        {
            Options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? new ConsoleLogger();
        }

        public HttpRequestMessage BuildRequest(string question, string? systemPrompt = null)
        {
            if (string.IsNullOrWhiteSpace(question))
            {
                throw new ArgumentException("A pergunta não pode ser vazia.", nameof(question));
            }

            var messages = new List<object>();
            if (!string.IsNullOrWhiteSpace(systemPrompt))
            {
                messages.Add(new { role = "system", content = systemPrompt });
            }

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
                request.Headers.TryAddWithoutValidation(
                    "Authorization",
                    "Bearer " + Options.ApiKey);

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
                {
                    detail = detail.Substring(0, 500);
                }

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

        /// <summary>
        /// Rodada única de chat com suporte a ferramentas (function calling).
        /// Devolve o texto final ou as chamadas de ferramenta solicitadas pelo
        /// modelo; o AgentSession executa as chamadas e faz o loop.
        /// </summary>
        public async Task<AgentChatResponse> ChatToolsAsync(
            List<AgentMessage> messages,
            List<AgentToolDefinition>? tools = null,
            HttpClient? httpClient = null,
            CancellationToken ct = default,
            string? systemPrompt = null)
        {
            EnsureValidApiKey();

            var payload = new JsonObject
            {
                ["model"] = Options.Model,
                ["max_tokens"] = Options.MaxTokens
            };

            var arr = new JsonArray();
            if (!string.IsNullOrWhiteSpace(systemPrompt))
            {
                arr.Add(new JsonObject { ["role"] = "system", ["content"] = systemPrompt });
            }

            if (messages != null)
            {
                foreach (AgentMessage m in messages)
                {
                    var mo = new JsonObject { ["role"] = m.Role };
                    if (m.Content != null)
                    {
                        mo["content"] = m.Content;
                    }

                    if (m.ToolCallId != null)
                    {
                        mo["tool_call_id"] = m.ToolCallId;
                    }

                    if (m.ToolCalls is { Count: > 0 })
                    {
                        var calls = new JsonArray();
                        foreach (AgentToolCall tc in m.ToolCalls)
                        {
                            calls.Add(new JsonObject
                            {
                                ["id"] = tc.Id,
                                ["type"] = "function",
                                ["function"] = new JsonObject
                                {
                                    ["name"] = tc.Name,
                                    ["arguments"] = tc.ArgumentsJson
                                }
                            });
                        }

                        mo["tool_calls"] = calls;
                    }

                    arr.Add(mo);
                }
            }

            payload["messages"] = arr;

            if (tools is { Count: > 0 })
            {
                var toolsArray = new JsonArray();
                foreach (AgentToolDefinition t in tools)
                {
                    var props = new JsonObject();
                    foreach (KeyValuePair<string, AgentToolParameter> p in t.Parameters)
                    {
                        props[p.Key] = new JsonObject
                        {
                            ["type"] = p.Value.Type,
                            ["description"] = p.Value.Description
                        };
                    }

                    var schema = new JsonObject { ["type"] = "object", ["properties"] = props };
                    if (t.Required.Count > 0)
                    {
                        var required = new JsonArray();
                        foreach (string r in t.Required)
                        {
                            required.Add(r);
                        }

                        schema["required"] = required;
                    }

                    toolsArray.Add(new JsonObject
                    {
                        ["type"] = "function",
                        ["function"] = new JsonObject
                        {
                            ["name"] = t.Name,
                            ["description"] = t.Description,
                            ["parameters"] = schema
                        }
                    });
                }

                payload["tools"] = toolsArray;
            }

            string json = JsonSerializer.Serialize(payload);
            HttpClient client = httpClient ?? ResolveClient();
            var request = new HttpRequestMessage(HttpMethod.Post, Options.BaseUrl);
            request.Headers.TryAddWithoutValidation("Authorization", "Bearer " + Options.ApiKey);
            if (Options.AppReference != null)
            {
                request.Headers.TryAddWithoutValidation("X-Title", "AURA");
                request.Headers.TryAddWithoutValidation("X-URL", Options.AppReference);
            }

            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.SendAsync(request, ct).ConfigureAwait(false);
            string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                string detail = string.IsNullOrWhiteSpace(body) ? "(sem corpo)" : body;
                if (detail.Length > 500)
                {
                    detail = detail.Substring(0, 500);
                }

                _logger.Error("LLM: " + response.StatusCode + " " + detail);
                return new AgentChatResponse
                {
                    Error = string.Format("Falha na chamada LLM ({0} {1}): {2}",
                        (int)response.StatusCode, response.StatusCode, detail)
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

                        // Ollama/Qwen pequeno pode retornar a chamada de ferramenta
                        // como JSON no campo content, em vez de usar tool_calls.
                        if (calls.Count == 0)
                        {
                            List<AgentToolCall>? textCalls = TryParseTextToolCall(content);

                            if (textCalls is { Count: > 0 })
                            {
                                return new AgentChatResponse
                                {
                                    Content = null,
                                    ToolCalls = textCalls
                                };
                            }
                        }

                        return new AgentChatResponse
                        {
                            Content = content,
                            ToolCalls = calls.Count > 0 ? calls : null
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

        private void EnsureValidApiKey()
        {
            if (string.Equals(Options.Provider, "ollama", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(Options.BaseUrl))
                {
                    throw new InvalidOperationException(
                        "Endpoint do Ollama não configurado.");
                }

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

            // Remove bloco Markdown ```json ... ```
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

                    // Alguns modelos retornam arguments como string JSON.
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
            {
                return value.GetString();
            }

            return null;
        }

        private static string? ReadContentString(JsonElement message)
        {
            if (!message.TryGetProperty("content", out JsonElement content))
            {
                return null;
            }

            if (content.ValueKind == JsonValueKind.String)
            {
                return content.GetString();
            }

            if (content.ValueKind == JsonValueKind.Array)
            {
                var sb = new StringBuilder();
                foreach (JsonElement part in content.EnumerateArray())
                {
                    if (part.TryGetProperty("text", out JsonElement text) &&
                        text.ValueKind == JsonValueKind.String)
                    {
                        sb.Append(text.GetString());
                    }
                }

                return sb.Length > 0 ? sb.ToString() : null;
            }

            return null;
        }
    }
}
