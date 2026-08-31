using AURA.AI;
using AURA.AI.Providers;

namespace AURA.Mobile.Diagnostics
{
    public static class RuntimeConfig
    {
        private const string ApiKeySecureName = "ai_api_key";
        private const string ApiKeyLegacyPref = "ai_api_key";
        private const string ApiKeyProviderPrefix = "ai_api_key_";

        public static int MaxTokens
        {
            get => Preferences.Default.Get("ai_max_tokens", 1500);
            set => Preferences.Default.Set("ai_max_tokens", value);
        }

        public static int TimeoutSeconds
        {
            get => Preferences.Default.Get("ai_timeout_seconds", 90);
            set => Preferences.Default.Set("ai_timeout_seconds", value);
        }

        public static int LogLinesForAnalysis
        {
            get => Preferences.Default.Get("ai_log_lines", 120);
            set => Preferences.Default.Set("ai_log_lines", value);
        }

        public static string Provider
        {
            get => Preferences.Default.Get("ai_provider", string.Empty);
            set => Preferences.Default.Set("ai_provider", (value ?? string.Empty).Trim());
        }

        public static string Model
        {
            get => Preferences.Default.Get("ai_model", string.Empty);
            set => Preferences.Default.Set("ai_model", (value ?? string.Empty).Trim());
        }

        public static string BaseUrlOverride
        {
            get => Preferences.Default.Get("ai_base_url", string.Empty);
            set => Preferences.Default.Set("ai_base_url", (value ?? string.Empty).Trim());
        }

        public static string ModelsUrlOverride
        {
            get => Preferences.Default.Get("ai_models_url", string.Empty);
            set => Preferences.Default.Set("ai_models_url", (value ?? string.Empty).Trim());
        }

        public static AiApiFormat ApiFormat
        {
            get
            {
                string value = Preferences.Default.Get("ai_api_format", string.Empty);
                return Enum.TryParse<AiApiFormat>(value, true, out var format)
                    ? format
                    : (ProviderCatalog.Find(Provider)?.ApiFormat ?? AiApiFormat.OpenAICompletions);
            }
            set => Preferences.Default.Set("ai_api_format", value.ToString());
        }

        public static string LastStatusMessage { get; private set; } = string.Empty;

        public static string ApiKey
        {
            get => GetApiKeyForProvider(Provider);
            set => SetApiKeyForProvider(Provider, value);
        }

        public static string GetApiKeyForProvider(string? providerId)
        {
            string provider = NormalizeProviderKey(providerId);
            if (string.IsNullOrWhiteSpace(provider)) return string.Empty;

            try
            {
                string? scoped = SecureStorage.Default.GetAsync(ApiKeyProviderPrefix + provider).GetAwaiter().GetResult();
                if (!string.IsNullOrWhiteSpace(scoped)) return scoped.Trim();
            }
            catch { }

            try
            {
                string? legacySecure = SecureStorage.Default.GetAsync(ApiKeySecureName).GetAwaiter().GetResult();
                if (!string.IsNullOrWhiteSpace(legacySecure))
                {
                    SetApiKeyForProvider(provider, legacySecure);
                    SecureStorage.Default.Remove(ApiKeySecureName);
                    return legacySecure.Trim();
                }
            }
            catch { }

            string legacy = Preferences.Default.Get(ApiKeyLegacyPref, string.Empty);
            if (string.IsNullOrWhiteSpace(legacy)) return string.Empty;
            SetApiKeyForProvider(provider, legacy);
            Preferences.Default.Remove(ApiKeyLegacyPref);
            return legacy.Trim();
        }

        public static void SetApiKeyForProvider(string? providerId, string? value)
        {
            string provider = NormalizeProviderKey(providerId);
            if (string.IsNullOrWhiteSpace(provider)) return;
            string v = (value ?? string.Empty).Trim();
            string secureName = ApiKeyProviderPrefix + provider;
            try
            {
                if (string.IsNullOrEmpty(v)) SecureStorage.Default.Remove(secureName);
                else SecureStorage.Default.SetAsync(secureName, v).GetAwaiter().GetResult();
            }
            catch { }
        }

        public static void ClearApiKey() => SetApiKeyForProvider(Provider, string.Empty);

        private static string NormalizeProviderKey(string? providerId)
        {
            string value = (providerId ?? string.Empty).Trim().ToLowerInvariant();
            if (value.Length == 0) return string.Empty;
            return new string(value.Select(c => char.IsLetterOrDigit(c) || c is '-' or '_' ? c : '_').ToArray());
        }

        public static string NormalizeChatBaseUrl(string? url, string? providerId)
        {
            string u = (url ?? string.Empty).Trim().TrimEnd('/');
            if (string.IsNullOrEmpty(u)) return string.Empty;
            if (u.Contains("/chat/completions", StringComparison.OrdinalIgnoreCase) ||
                u.Contains("/messages", StringComparison.OrdinalIgnoreCase) ||
                u.Contains("/api/chat", StringComparison.OrdinalIgnoreCase)) return u;
            if (string.Equals(providerId, "ollama", StringComparison.OrdinalIgnoreCase) ||
                u.Contains("127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
                u.Contains("localhost", StringComparison.OrdinalIgnoreCase)) return u + "/v1/chat/completions";
            return u;
        }

        public static void Apply(OpenRouterClient client)
        {
            ProviderInfo provider = ProviderCatalog.Find(Provider) ?? ProviderCatalog.Providers[0];
            string model = Model;
            if (string.IsNullOrWhiteSpace(model) && provider.Models.Count > 0)
                model = !string.IsNullOrWhiteSpace(provider.DefaultModelId) ? provider.DefaultModelId : provider.Models[0].Id;

            string baseUrl = !string.IsNullOrWhiteSpace(BaseUrlOverride)
                ? NormalizeChatBaseUrl(BaseUrlOverride, provider.Id)
                : NormalizeChatBaseUrl(provider.BaseUrl, provider.Id);

            client.Options.Provider = provider.Id;
            client.Options.BaseUrl = baseUrl;
            client.Options.Model = model;
            client.Options.MaxTokens = MaxTokens;
            client.Options.TimeoutSeconds = TimeoutSeconds;
            client.Options.ApiKey = provider.NeedsKey ? GetApiKeyForProvider(provider.Id) : string.Empty;
            client.Options.AuthHeaderName = provider.AuthHeaderName ?? string.Empty;
            client.Options.AuthScheme = provider.AuthScheme ?? string.Empty;
            client.Options.ApiFormat = ApiFormat;
            LastStatusMessage = provider.Name + " · " + model + " · " + baseUrl;
        }

        public static string? ValidateApiKeyFormat(string? key, ProviderInfo? provider)
        {
            if (provider == null || !provider.NeedsKey) return null;
            string k = (key ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(k)) return null;
            if (k.Length > 4096 || k.IndexOfAny(new[] { ' ', '\t', '\r', '\n' }) >= 0)
                return "Chave de API inválida. Cole somente a chave, sem espaços ou quebras de linha.";
            return null;
        }

        public static string? EnsureReadyForRequest(OpenRouterClient client)
        {
            Apply(client);
            ProviderInfo provider = ProviderCatalog.Find(Provider) ?? ProviderCatalog.Providers[0];
            string key = client.Options.ApiKey ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(key)) return ValidateApiKeyFormat(key, provider);
            if (!provider.NeedsKey) return null;
            return "Configure a chave de API do provedor selecionado.";
        }
    }
}
