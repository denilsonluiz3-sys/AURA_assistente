using AURA.AI.Providers;

namespace AURA.AI.UniversalAI;

/// <summary>Adapta a configuração universal ao cliente de IA existente da AURA.</summary>
public static class UniversalAiClientFactory
{
    public static OpenRouterClient Create(UniversalConnection connection, int maxTokens = 1500, int timeoutSeconds = 90)
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
            ApiKey = connection.ApiKey.Trim(),
            BaseUrl = connection.Provider.BaseUrl,
            Model = connection.Model,
            MaxTokens = maxTokens,
            TimeoutSeconds = timeoutSeconds,
            AuthHeaderName = connection.Provider.AuthHeader,
            AuthScheme = connection.Provider.AuthScheme,
            ApiFormat = MapFormat(connection.Provider.Format)
        });
    }

    private static AiApiFormat MapFormat(UniversalApiFormat format) => format switch
    {
        UniversalApiFormat.AnthropicMessages => AiApiFormat.AnthropicMessages,
        UniversalApiFormat.Gemini => AiApiFormat.GeminiGenerateContent,
        _ => AiApiFormat.OpenAICompletions
    };
}
