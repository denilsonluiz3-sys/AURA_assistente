using AURA.AI.Providers;

namespace AURA.AI.UniversalAI;

/// <summary>Adapta uma conexão universal ao cliente HTTP existente da AURA.</summary>
public static class UniversalAiClientFactory
{
    public static OpenRouterClient Create(UniversalConnection connection, int maxTokens = 1500, int timeoutSeconds = 90)
    {
        if (connection is null) throw new ArgumentNullException(nameof(connection));
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
            AuthHeaderName = connection.Provider.AuthHeader,
            AuthScheme = NormalizeScheme(connection.Provider.AuthScheme),
            ApiFormat = MapFormat(connection.Provider.Format)
        });
    }

    private static AiApiFormat MapFormat(UniversalApiFormat format) => format switch
    {
        UniversalApiFormat.AnthropicMessages => AiApiFormat.AnthropicMessages,
        UniversalApiFormat.Gemini => AiApiFormat.OpenAICompletions,
        _ => AiApiFormat.OpenAICompletions
    };

    private static string NormalizeScheme(string? scheme)
    {
        string value = (scheme ?? string.Empty).Trim();
        return value.Length == 0 ? string.Empty : value + " ";
    }
}
