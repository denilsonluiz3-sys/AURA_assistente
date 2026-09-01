namespace AURA.AI.UniversalAI;

/// <summary>Cria uma conexão universal sem alterar o contrato do AgentSession.</summary>
public static class UniversalRuntimeAdapter
{
    public static UniversalConnection CreateConnection(string providerId, string apiKey, string model, string? baseUrl = null, string? modelsUrl = null)
    {
        var provider = UniversalProviderRegistry.BuiltIns.FirstOrDefault(p => string.Equals(p.Id, providerId, StringComparison.OrdinalIgnoreCase));
        if (provider is null)
        {
            if (string.IsNullOrWhiteSpace(baseUrl)) throw new ArgumentException("Base URL obrigatória para provider customizado.", nameof(baseUrl));
            provider = UniversalProviderRegistry.Custom(baseUrl, modelsUrl);
        }
        else if (!string.IsNullOrWhiteSpace(baseUrl) || !string.IsNullOrWhiteSpace(modelsUrl))
        {
            provider = provider with
            {
                BaseUrl = string.IsNullOrWhiteSpace(baseUrl) ? provider.BaseUrl : baseUrl.TrimEnd('/'),
                ModelsUrl = string.IsNullOrWhiteSpace(modelsUrl) ? provider.ModelsUrl : modelsUrl.TrimEnd('/')
            };
        }
        return new UniversalConnection(provider, apiKey ?? string.Empty, model ?? string.Empty);
    }
}
