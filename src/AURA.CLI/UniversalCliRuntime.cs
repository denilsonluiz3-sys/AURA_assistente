using AURA.AI.UniversalAI;

namespace AURA.CLI;

internal static class UniversalCliRuntime
{
    public static IUniversalAiClient Create(string? modelOverride = null)
    {
        string provider = Get("AURA_PROVIDER");
        string model = modelOverride ?? Get("AURA_MODEL");
        string endpoint = Get("AURA_BASE_URL");
        string modelsUrl = Get("AURA_MODELS_URL");
        string key = Get("AURA_API_KEY");
        var format = Enum.TryParse<UniversalApiFormat>(Get("AURA_API_FORMAT"), true, out var parsed) ? parsed : UniversalApiFormat.OpenAiCompatible;
        string header = Environment.GetEnvironmentVariable("AURA_AUTH_HEADER") ?? "Authorization";
        string scheme = Environment.GetEnvironmentVariable("AURA_AUTH_SCHEME") ?? "Bearer";
        bool requiresKey = !bool.TryParse(Environment.GetEnvironmentVariable("AURA_REQUIRES_API_KEY"), out var required) || required;
        var connection = UniversalRuntimeAdapter.CreateConnection(provider, key, model, endpoint, modelsUrl, format, header, scheme, requiresKey);
        return UniversalAiClientFactory.Create(connection, maxTokens: ReadInt("AURA_MAX_TOKENS", 1500), timeoutSeconds: ReadInt("AURA_TIMEOUT_SECONDS", 90));
    }

    private static string Get(string name) => Environment.GetEnvironmentVariable(name)?.Trim() ?? string.Empty;
    private static int ReadInt(string name, int fallback) => int.TryParse(Environment.GetEnvironmentVariable(name), out var value) && value > 0 ? value : fallback;
}
