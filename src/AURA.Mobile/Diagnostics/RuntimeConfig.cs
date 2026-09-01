using AURA.AI.UniversalAI;

namespace AURA.Mobile.Diagnostics;

public static class RuntimeConfig
{
    private const string ApiKeyPrefix = "ai_api_key_";
    public static int MaxTokens { get => Preferences.Default.Get("ai_max_tokens", 1500); set => Preferences.Default.Set("ai_max_tokens", value); }
    public static int TimeoutSeconds { get => Preferences.Default.Get("ai_timeout_seconds", 90); set => Preferences.Default.Set("ai_timeout_seconds", value); }
    public static int LogLinesForAnalysis { get => Preferences.Default.Get("ai_log_lines", 120); set => Preferences.Default.Set("ai_log_lines", value); }
    public static string Provider { get => Preferences.Default.Get("ai_provider", string.Empty); set => Preferences.Default.Set("ai_provider", (value ?? string.Empty).Trim()); }
    public static string Model { get => Preferences.Default.Get("ai_model", string.Empty); set => Preferences.Default.Set("ai_model", (value ?? string.Empty).Trim()); }
    public static string BaseUrlOverride { get => Preferences.Default.Get("ai_base_url", string.Empty); set => Preferences.Default.Set("ai_base_url", (value ?? string.Empty).Trim()); }
    public static string ModelsUrlOverride { get => Preferences.Default.Get("ai_models_url", string.Empty); set => Preferences.Default.Set("ai_models_url", (value ?? string.Empty).Trim()); }
    public static UniversalApiFormat ApiFormat { get => Enum.TryParse<UniversalApiFormat>(Preferences.Default.Get("ai_api_format", string.Empty), true, out var f) ? f : UniversalApiFormat.OpenAiCompatible; set => Preferences.Default.Set("ai_api_format", value.ToString()); }
    public static string AuthHeader { get => Preferences.Default.Get("ai_auth_header", "Authorization"); set => Preferences.Default.Set("ai_auth_header", (value ?? "").Trim()); }
    public static string AuthScheme { get => Preferences.Default.Get("ai_auth_scheme", "Bearer"); set => Preferences.Default.Set("ai_auth_scheme", (value ?? "").Trim()); }
    public static bool RequiresApiKey { get => Preferences.Default.Get("ai_requires_key", true); set => Preferences.Default.Set("ai_requires_key", value); }
    public static string LastStatusMessage { get; private set; } = string.Empty;
    public static string ApiKey { get => GetApiKeyForProvider(Provider); set => SetApiKeyForProvider(Provider, value); }
    public static string GetApiKeyForProvider(string? providerId) { var id = Normalize(providerId); if (string.IsNullOrEmpty(id)) return string.Empty; try { return SecureStorage.Default.GetAsync(ApiKeyPrefix + id).GetAwaiter().GetResult()?.Trim() ?? string.Empty; } catch { return string.Empty; } }
    public static void SetApiKeyForProvider(string? providerId, string? value) { var id = Normalize(providerId); if (string.IsNullOrEmpty(id)) return; try { var key = ApiKeyPrefix + id; var v = (value ?? string.Empty).Trim(); if (v.Length == 0) SecureStorage.Default.Remove(key); else SecureStorage.Default.SetAsync(key, v).GetAwaiter().GetResult(); } catch { } }
    public static void ClearApiKey() => SetApiKeyForProvider(Provider, string.Empty);
    public static string NormalizeChatBaseUrl(string? url, string? providerId = null) => (url ?? string.Empty).Trim().TrimEnd('/');
    public static UniversalConnection CreateConnection() { var provider = Provider.Trim(); var model = Model.Trim(); var baseUrl = NormalizeChatBaseUrl(BaseUrlOverride); if (string.IsNullOrWhiteSpace(provider)) throw new InvalidOperationException("Provider não configurado."); if (string.IsNullOrWhiteSpace(model)) throw new InvalidOperationException("Modelo não configurado."); if (string.IsNullOrWhiteSpace(baseUrl)) throw new InvalidOperationException("Endpoint não configurado."); return UniversalRuntimeAdapter.CreateConnection(provider, GetApiKeyForProvider(provider), model, baseUrl, ModelsUrlOverride, ApiFormat, AuthHeader, AuthScheme, RequiresApiKey); }
    public static void Apply(IUniversalAiClient client) { ArgumentNullException.ThrowIfNull(client); var c = CreateConnection(); var configured = UniversalAiClientFactory.Create(c, MaxTokens, TimeoutSeconds); client.Options.Provider = configured.Options.Provider; client.Options.ApiKey = configured.Options.ApiKey; client.Options.BaseUrl = configured.Options.BaseUrl; client.Options.Model = configured.Options.Model; client.Options.MaxTokens = configured.Options.MaxTokens; client.Options.TimeoutSeconds = configured.Options.TimeoutSeconds; client.Options.AuthHeaderName = configured.Options.AuthHeaderName; client.Options.AuthScheme = configured.Options.AuthScheme; client.Options.ApiFormat = configured.Options.ApiFormat; LastStatusMessage = c.Provider.Name + " · " + c.Model + " · " + c.Provider.BaseUrl; }
    public static string? EnsureReadyForRequest(IUniversalAiClient client) { if (client == null) return "Cliente de IA não configurado."; if (string.IsNullOrWhiteSpace(client.Options.BaseUrl)) return "Configure o endpoint do provider."; if (string.IsNullOrWhiteSpace(client.Options.Model)) return "Configure o modelo."; if (RequiresApiKey && string.IsNullOrWhiteSpace(client.Options.ApiKey)) return "Configure a chave de API do provider."; return null; }
    public static IUniversalAiClient CreateClient() { var c = CreateConnection(); return UniversalAiClientFactory.Create(c, MaxTokens, TimeoutSeconds); }
    private static string Normalize(string? value) { var s = (value ?? string.Empty).Trim().ToLowerInvariant(); return s.Length == 0 ? string.Empty : new string(s.Select(c => char.IsLetterOrDigit(c) || c is '-' or '_' ? c : '_').ToArray()); }
}
