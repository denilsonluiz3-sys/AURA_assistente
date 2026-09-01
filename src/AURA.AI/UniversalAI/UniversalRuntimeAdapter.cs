namespace AURA.AI.UniversalAI;

/// <summary>Adaptador entre configurações externas e a conexão universal do runtime.</summary>
public static class UniversalRuntimeAdapter
{
    public static UniversalConnection CreateConnection(
        string providerId,
        string apiKey,
        string model,
        string? baseUrl = null,
        string? modelsUrl = null)
    {
        return UniversalAiRuntime.Resolve(providerId, apiKey, model, baseUrl, modelsUrl);
    }
}
