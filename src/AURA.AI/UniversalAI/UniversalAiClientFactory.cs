namespace AURA.AI.UniversalAI;

/// <summary>Único ponto autorizado a transformar configuração universal em cliente executável.</summary>
public static class UniversalAiClientFactory
{
    public static IUniversalAiClient Create(UniversalConnection connection, int maxTokens = 1500, int timeoutSeconds = 90)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(connection.Provider);
        if (connection.Provider.RequiresApiKey && string.IsNullOrWhiteSpace(connection.ApiKey))
            throw new ArgumentException("API key obrigatória para o provider configurado.", nameof(connection));
        if (string.IsNullOrWhiteSpace(connection.Model))
            throw new ArgumentException("Modelo obrigatório.", nameof(connection));
        if (string.IsNullOrWhiteSpace(connection.Provider.BaseUrl))
            throw new ArgumentException("Endpoint obrigatório.", nameof(connection));

        return new UniversalAiClient(new UniversalAiClientOptions
        {
            Provider = connection.Provider.Id,
            ApiKey = connection.ApiKey.Trim(),
            BaseUrl = connection.Provider.BaseUrl.Trim(),
            Model = connection.Model.Trim(),
            MaxTokens = Math.Max(1, maxTokens),
            TimeoutSeconds = Math.Max(1, timeoutSeconds),
            AuthHeaderName = connection.Provider.AuthHeader,
            AuthScheme = connection.Provider.AuthScheme,
            ApiFormat = connection.Provider.Format
        });
    }
}
