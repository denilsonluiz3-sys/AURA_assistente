using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AURA.AI.UniversalAI;

/// <summary>Cliente HTTP agnóstico ao provider. O protocolo é determinado pela configuração.</summary>
public sealed class UniversalAiClient : IUniversalAiClient
{
    public UniversalAiClientOptions Options { get; }

    public UniversalAiClient(UniversalAiClientOptions options)
        => Options = options ?? throw new ArgumentNullException(nameof(options));

    public async Task<string> ChatAsync(string question, HttpClient? httpClient = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(question)) throw new ArgumentException("Pergunta obrigatória.", nameof(question));
        var response = await SendAsync(new[] { new AgentMessage { Role = "user", Content = question } }, Array.Empty<AgentToolDefinition>(), httpClient, ct, null).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(response.Error)) throw new AgentLlmException(response.Error, response.ErrorKind);
        return response.Content ?? string.Empty;
    }

    public Task<AgentChatResponse> ChatToolsAsync(IReadOnlyList<AgentMessage> messages, IReadOnlyList<AgentToolDefinition> tools, HttpClient? httpClient = null, CancellationToken ct = default, string? systemPrompt = null)
        => SendAsync(messages, tools, httpClient, ct, systemPrompt);

    private async Task<AgentChatResponse> SendAsync(IReadOnlyList<AgentMessage> messages, IReadOnlyList<AgentToolDefinition> tools, HttpClient? httpClient, CancellationToken ct, string? systemPrompt)
    {
        Validate();
        using var own = httpClient == null ? new HttpClient() : null;
        var http = httpClient ?? own!;
        using var request = new HttpRequestMessage(HttpMethod.Post, Options.BaseUrl);
        AddAuthentication(request);
        request.Content = new StringContent(BuildPayload(messages, tools, systemPrompt), Encoding.UTF8, "application/json");
        try
        {
            using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
            var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                return new AgentChatResponse { Error = $"Falha HTTP {(int)response.StatusCode}: {Truncate(body)}", ErrorKind = Classify(response.StatusCode) };
            return ParseResponse(body);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        { return new AgentChatResponse { Error = "Tempo limite da requisição.", ErrorKind = AgentErrorKind.Timeout }; }
        catch (HttpRequestException ex)
        { return new AgentChatResponse { Error = "Falha de rede: " + ex.Message, ErrorKind = AgentErrorKind.Network }; }
    }

    private void Validate()
    {
        if (string.IsNullOrWhiteSpace(Options.BaseUrl)) throw new InvalidOperationException("Endpoint não configurado.");
        if (string.IsNullOrWhiteSpace(Options.Model)) throw new InvalidOperationException("Modelo não configurado.");
        if (Options.ApiFormat != UniversalApiFormat.OpenAiCompatible && Options.ApiFormat != UniversalApiFormat.AnthropicMessages && Options.ApiFormat != UniversalApiFormat.Gemini)
            throw new InvalidOperationException("Formato de API não suportado.");
        if (Options.ApiFormat != UniversalApiFormat.OpenAiCompatible && string.IsNullOrWhiteSpace(Options.AuthHeaderName) && !string.IsNullOrWhiteSpace(Options.ApiKey))
            throw new InvalidOperationException("Header de autenticação não configurado.");
    }

    private void AddAuthentication(HttpRequestMessage request)
    {
        if (string.IsNullOrWhiteSpace(Options.ApiKey)) return;
        var header = string.IsNullOrWhiteSpace(Options.AuthHeaderName) ? "Authorization" : Options.AuthHeaderName;
        var scheme = Options.AuthScheme?.Trim() ?? string.Empty;
        var value = string.IsNullOrEmpty(scheme) ? Options.ApiKey.Trim() : scheme + " " + Options.ApiKey.Trim();
        request.Headers.TryAddWithoutValidation(header, value);
    }

    private string BuildPayload(IReadOnlyList<AgentMessage> messages, IReadOnlyList<AgentToolDefinition> tools, string? systemPrompt)
    {
        return Options.ApiFormat switch
        {
            UniversalApiFormat.AnthropicMessages => BuildAnthropic(messages, tools, systemPrompt),
            UniversalApiFormat.Gemini => BuildGemini(messages, tools, systemPrompt),
            _ => BuildOpenAi(messages, tools, systemPrompt)
        };
    }

    private string BuildOpenAi(IReadOnlyList<AgentMessage> messages, IReadOnlyList<AgentToolDefinition> tools, string? systemPrompt)
    {
        var list = new List<object>();
        if (!string.IsNullOrWhiteSpace(systemPrompt)) list.Add(new { role = "system", content = systemPrompt });
        foreach (var m in messages)
        {
            var item = new Dictionary<string, object?> { ["role"] = m.Role, ["content"] = m.Content };
            if (!string.IsNullOrWhiteSpace(m.ToolCallId)) item["tool_call_id"] = m.ToolCallId;
            if (m.ToolCalls is { Count: > 0 }) item["tool_calls"] = m.ToolCalls.Select(t => new { id = t.Id, type = "function", function = new { name = t.Name, arguments = t.ArgumentsJson } }).ToArray();
            list.Add(item);
        }
        var toolObjects = tools.Select(t => new { type = "function", function = new { name = t.Name, description = t.Description, parameters = new { type = "object", properties = t.Parameters.ToDictionary(p => p.Key, p => new { type = p.Value.Type, description = p.Value.Description }), required = t.Required } } }).ToArray();
        var payload = new Dictionary<string, object?> { ["model"] = Options.Model, ["messages"] = list, ["max_tokens"] = Options.MaxTokens };
        if (toolObjects.Length > 0) payload["tools"] = toolObjects;
        return JsonSerializer.Serialize(payload);
    }

    private string BuildAnthropic(IReadOnlyList<AgentMessage> messages, IReadOnlyList<AgentToolDefinition> tools, string? systemPrompt)
    {
        var list = messages.Where(m => m.Role != "system").Select(m => new { role = m.Role == "assistant" ? "assistant" : "user", content = m.Content ?? string.Empty }).ToArray();
        var payload = new Dictionary<string, object?> { ["model"] = Options.Model, ["messages"] = list, ["max_tokens"] = Options.MaxTokens };
        if (!string.IsNullOrWhiteSpace(systemPrompt)) payload["system"] = systemPrompt;
        if (tools.Count > 0) payload["tools"] = tools.Select(t => new { name = t.Name, description = t.Description, input_schema = new { type = "object", properties = t.Parameters.ToDictionary(p => p.Key, p => new { type = p.Value.Type, description = p.Value.Description }), required = t.Required } }).ToArray();
        return JsonSerializer.Serialize(payload);
    }

    private string BuildGemini(IReadOnlyList<AgentMessage> messages, IReadOnlyList<AgentToolDefinition> tools, string? systemPrompt)
    {
        var contents = messages.Where(m => m.Role != "system").Select(m => new { role = m.Role == "assistant" ? "model" : "user", parts = new[] { new { text = m.Content ?? string.Empty } } }).ToArray();
        var payload = new Dictionary<string, object?> { ["contents"] = contents, ["generationConfig"] = new { maxOutputTokens = Options.MaxTokens } };
        if (!string.IsNullOrWhiteSpace(systemPrompt)) payload["systemInstruction"] = new { parts = new[] { new { text = systemPrompt } } };
        return JsonSerializer.Serialize(payload);
    }

    private static AgentChatResponse ParseResponse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        if (root.TryGetProperty("choices", out var choices) && choices.ValueKind == JsonValueKind.Array && choices.GetArrayLength() > 0)
        {
            var msg = choices[0].GetProperty("message");
            var result = new AgentChatResponse { Content = msg.TryGetProperty("content", out var c) && c.ValueKind != JsonValueKind.Null ? c.GetString() : null };
            if (msg.TryGetProperty("tool_calls", out var tc) && tc.ValueKind == JsonValueKind.Array)
            {
                result.ToolCalls = new List<AgentToolCall>();
                foreach (var item in tc.EnumerateArray())
                {
                    var fn = item.GetProperty("function");
                    result.ToolCalls.Add(new AgentToolCall { Id = item.GetProperty("id").GetString() ?? Guid.NewGuid().ToString("N"), Name = fn.GetProperty("name").GetString() ?? string.Empty, ArgumentsJson = fn.GetProperty("arguments").GetString() ?? "{}" });
                }
            }
            return result;
        }
        if (root.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.Array)
            return new AgentChatResponse { Content = string.Join("\n", content.EnumerateArray().Where(x => x.TryGetProperty("text", out _)).Select(x => x.GetProperty("text").GetString())) };
        if (root.TryGetProperty("candidates", out var candidates) && candidates.ValueKind == JsonValueKind.Array && candidates.GetArrayLength() > 0)
        {
            var parts = candidates[0].GetProperty("content").GetProperty("parts");
            return new AgentChatResponse { Content = string.Join("\n", parts.EnumerateArray().Where(x => x.TryGetProperty("text", out _)).Select(x => x.GetProperty("text").GetString())) };
        }
        return new AgentChatResponse { Error = "Resposta do provider sem conteúdo reconhecível.", ErrorKind = AgentErrorKind.Unknown };
    }

    private static AgentErrorKind Classify(System.Net.HttpStatusCode code) => (int)code switch { 400 => AgentErrorKind.InvalidRequest, 401 => AgentErrorKind.InvalidApiKey, 402 => AgentErrorKind.PaymentRequired, 429 => AgentErrorKind.RateLimited, >= 500 => AgentErrorKind.ProviderError, _ => AgentErrorKind.Unknown };
    private static string Truncate(string value) => value.Length <= 1000 ? value : value[..1000];
}
