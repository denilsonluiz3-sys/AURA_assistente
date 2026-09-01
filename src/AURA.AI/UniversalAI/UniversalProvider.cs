namespace AURA.AI.UniversalAI;

public enum UniversalApiFormat
{
    OpenAiCompatible,
    AnthropicMessages,
    Gemini
}

public sealed record UniversalProvider(
    string Id,
    string Name,
    string BaseUrl,
    string ModelsUrl,
    UniversalApiFormat Format = UniversalApiFormat.OpenAiCompatible,
    string AuthHeader = "Authorization",
    string AuthScheme = "Bearer",
    bool RequiresApiKey = true);

public sealed record UniversalModel(string Id, string DisplayName, string ProviderId, string? OwnedBy = null);

public sealed record UniversalConnection(UniversalProvider Provider, string ApiKey, string Model);
