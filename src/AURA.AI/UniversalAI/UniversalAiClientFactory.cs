using AURA.AI.Providers;
using AURA.Core.Logging;

namespace AURA.AI.UniversalAI;

/// <summary>Única fábrica entre a configuração universal e o cliente usado pelo AgentSession.</summary>
public static class UniversalAiClientFactory
{
    public static OpenRouterClient Create(
        UniversalConnection connection,
        int maxTokens = 1500,
        int timeoutSeconds = 90,
        ILogger? logger = null)
    {
        if (connection is null) throw new ArgumentNullException(nameof(connection));
        if (connection.Provider is null) throw new ArgumentException("Provider obrigatório.", nameof(connection));
        if (connection.Provider.RequiresApiKey && string.IsNullOrWhiteSpace(connection.ApiKey))
            throw new ArgumentException("API key obrigatória para este provider.", nameof(connection));
        if (string.IsNullOrWhiteSpace(connection.Model))
            throw new ArgumentException("Modelo obrigatório.", nameof(connection));
        if (string.IsNullOrWhiteSpace(connection.Provider.BaseUrl))
            throw new ArgumentException("Endpoint do provider obrigatório.", nameof(connection));

        return new OpenRouterClient(new OpenRouterOptions
        {
            Provider = connection.Provider.Id,
            ApiKey = connection.ApiKey?.Trim() ?? string.Empty,
            BaseUrl = connection.Provider.BaseUrl,
            Model = connection.Model.Trim(),
            MaxTokens = maxTokens,
            TimeoutSeconds = timeoutSeconds,
            AppReference = "AURA",
            AuthHeaderName = connection.Provider.AuthHeader ?? string.Empty,
            AuthScheme = NormalizeScheme(connection.Provider.AuthScheme),
            ApiFormat = MapFormat(connection.Provider.Format)
        }, logger);
    }

    private static AiApiFormat MapFormat(UniversalApiFormat format) => format switch
    {
        UniversalApiFormat.AnthropicMessages => AiApiFormat.AnthropicMessages,
        // Gemini is registered in config/providers.json through its OpenAI-compatible
        // endpoint. There is intentionally no separate GeminiGenerateContent value.
        UniversalApiFormat.Gemini => AiApiFormat.OpenAICompletions,
        _ => AiApiFormat.OpenAICompletions
    };

    private static string NormalizeScheme(string? scheme)
    {
        string value = (scheme ?? string.Empty).Trim();
        return value.Length == 0 ? string.Empty : value + " ";
    }
}
