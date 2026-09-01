using System.Net.Http;

namespace AURA.AI.UniversalAI;

/// <summary>Contrato único para comunicação com qualquer provider configurado pelo usuário.</summary>
public interface IUniversalAiClient
{
    UniversalAiClientOptions Options { get; }
    Task<string> ChatAsync(string question, HttpClient? httpClient = null, CancellationToken ct = default);
    Task<AgentChatResponse> ChatToolsAsync(IReadOnlyList<AgentMessage> messages, IReadOnlyList<AgentToolDefinition> tools, HttpClient? httpClient = null, CancellationToken ct = default, string? systemPrompt = null);
}

public sealed class UniversalAiClientOptions
{
    public string Provider { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int MaxTokens { get; set; } = 1500;
    public int TimeoutSeconds { get; set; } = 90;
    public string AuthHeaderName { get; set; } = "Authorization";
    public string AuthScheme { get; set; } = "Bearer";
    public UniversalApiFormat ApiFormat { get; set; } = UniversalApiFormat.OpenAiCompatible;
}
