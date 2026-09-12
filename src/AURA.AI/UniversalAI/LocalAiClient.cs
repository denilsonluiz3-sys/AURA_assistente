namespace AURA.AI.UniversalAI;

/// <summary>
/// Runtime de inferência local. A implementação concreta pode usar um backend
/// nativo Android sem acoplar o AgentSession a uma biblioteca de modelos.
/// </summary>
public interface ILocalAiRuntime
{
    Task<string> CompleteAsync(
        IReadOnlyList<AgentMessage> messages,
        IReadOnlyList<AgentToolDefinition> tools,
        CancellationToken ct = default);
}

/// <summary>
/// Adapta a saída textual de um runtime local ao contrato universal da AURA.
/// Respostas JSON de tool call entram no mesmo AgentSession usado online.
/// </summary>
public sealed class LocalAiClient : IUniversalAiClient
{
    private readonly ILocalAiRuntime _runtime;

    public LocalAiClient(ILocalAiRuntime runtime, UniversalAiClientOptions? options = null)
    {
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        Options = options ?? new UniversalAiClientOptions
        {
            Provider = "local",
            Model = "local",
            ApiFormat = UniversalApiFormat.OpenAiCompatible,
            AuthScheme = string.Empty,
            AuthHeaderName = string.Empty
        };
    }

    public UniversalAiClientOptions Options { get; }

    public async Task<string> ChatAsync(
        string question,
        HttpClient? httpClient = null,
        string? systemPrompt = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(question))
            throw new ArgumentException("A pergunta não pode ser vazia.", nameof(question));

        var messages = new List<AgentMessage>();
        if (!string.IsNullOrWhiteSpace(systemPrompt))
            messages.Add(new AgentMessage { Role = "system", Content = systemPrompt });
        messages.Add(new AgentMessage { Role = "user", Content = question });
        return await _runtime.CompleteAsync(messages, Array.Empty<AgentToolDefinition>(), ct).ConfigureAwait(false);
    }

    public async Task<AgentChatResponse> ChatToolsAsync(
        IReadOnlyList<AgentMessage> messages,
        IReadOnlyList<AgentToolDefinition> tools,
        HttpClient? httpClient = null,
        CancellationToken ct = default,
        string? systemPrompt = null)
    {
        var requestMessages = messages.ToList();
        if (!string.IsNullOrWhiteSpace(systemPrompt) &&
            !requestMessages.Any(x => string.Equals(x.Role, "system", StringComparison.OrdinalIgnoreCase)))
        {
            requestMessages.Insert(0, new AgentMessage { Role = "system", Content = systemPrompt });
        }

        string raw = await _runtime.CompleteAsync(requestMessages, tools, ct).ConfigureAwait(false);
        if (LocalToolCallParser.TryParseMany(raw, out var calls, out _))
        {
            return new AgentChatResponse
            {
                ToolCalls = calls.ToList(),
                Content = null
            };
        }

        return new AgentChatResponse { Content = raw };
    }
}
