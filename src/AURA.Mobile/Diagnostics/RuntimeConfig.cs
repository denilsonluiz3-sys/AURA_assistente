using AURA.AI;

namespace AURA.Mobile.Diagnostics
{
    public static class RuntimeConfig
    {
        private const string ApiKeySecureName = "ai_api_key";
        private const string ApiKeyLegacyPref = "ai_api_key";

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
            set => Preferences.Default.Set("ai_provider", value);
        }

        public static string Model
        {
            get => Preferences.Default.Get("ai_model", string.Empty);
            set => Preferences.Default.Set("ai_model", value);
        }

        public static string BaseUrlOverride
        {
            get => Preferences.Default.Get("ai_base_url", string.Empty);
            set => Preferences.Default.Set("ai_base_url", (value ?? string.Empty).Trim());
        }

        public static string LastStatusMessage { get; private set; } = string.Empty;

        public static string ApiKey
        {
            get
            {
                try
                {
                    string? secure = SecureStorage.Default.GetAsync(ApiKeySecureName).GetAwaiter().GetResult();
                    if (!string.IsNullOrWhiteSpace(secure))
                        return secure.Trim();
                }
                catch { }

                string legacy = Preferences.Default.Get(ApiKeyLegacyPref, string.Empty);
                if (string.IsNullOrWhiteSpace(legacy))
                    return string.Empty;

                try
                {
                    SecureStorage.Default.SetAsync(ApiKeySecureName, legacy.Trim()).GetAwaiter().GetResult();
                    Preferences.Default.Remove(ApiKeyLegacyPref);
                }
                catch { }

                return legacy.Trim();
            }
            set
            {
                string v = (value ?? string.Empty).Trim();
                try
                {
                    if (string.IsNullOrEmpty(v))
                        SecureStorage.Default.Remove(ApiKeySecureName);
                    else
                        SecureStorage.Default.SetAsync(ApiKeySecureName, v).GetAwaiter().GetResult();
                }
                catch { }

                try { Preferences.Default.Remove(ApiKeyLegacyPref); }
                catch { }
            }
        }

        public static void ClearApiKey() => ApiKey = string.Empty;

        public static string NormalizeChatBaseUrl(string? url, string? providerId)
        {
            string u = (url ?? string.Empty).Trim().TrimEnd('/');
            if (string.IsNullOrEmpty(u))
                return string.Empty;

            if (u.Contains("/chat/completions", StringComparison.OrdinalIgnoreCase) ||
                u.Contains("/messages", StringComparison.OrdinalIgnoreCase) ||
                u.Contains("/api/chat", StringComparison.OrdinalIgnoreCase))
                return u;

            if (string.Equals(providerId, "ollama", StringComparison.OrdinalIgnoreCase) ||
                u.Contains("127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
                u.Contains("localhost", StringComparison.OrdinalIgnoreCase))
                return u + "/v1/chat/completions";

            return u;
        }

        public static void Apply(OpenRouterClient client)
        {
            ProviderInfo provider = ProviderCatalog.Find(Provider) ?? ProviderCatalog.Providers[0];
            string model = Model;

            bool modelBelongsToProvider = false;
            if (!string.IsNullOrWhiteSpace(model))
            {
                foreach (ProviderModel m in provider.Models)
                {
                    if (string.Equals(m.Id, model, StringComparison.OrdinalIgnoreCase))
                    {
                        modelBelongsToProvider = true;
                        break;
                    }
                }

                // Custom models are valid; the catalog is not an allow-list.
                if (!modelBelongsToProvider && model.Length >= 2)
                    modelBelongsToProvider = true;
            }

            if (!modelBelongsToProvider && provider.Models.Count > 0)
            {
                model = !string.IsNullOrWhiteSpace(provider.DefaultModelId)
                    ? provider.DefaultModelId
                    : provider.Models[0].Id;
            }

            string baseUrl = provider.BaseUrl;
            string ovr = BaseUrlOverride;
            if (!string.IsNullOrWhiteSpace(ovr))
                baseUrl = NormalizeChatBaseUrl(ovr, provider.Id);
            else
                baseUrl = NormalizeChatBaseUrl(baseUrl, provider.Id);

            client.Options.Provider = provider.Id;
            client.Options.BaseUrl = baseUrl;
            client.Options.Model = model;
            client.Options.MaxTokens = MaxTokens;
            client.Options.TimeoutSeconds = TimeoutSeconds;
            client.Options.ApiKey = provider.NeedsKey ? (ApiKey?.Trim() ?? string.Empty) : string.Empty;
            client.Options.AuthHeaderName = provider.AuthHeaderName ?? string.Empty;
            client.Options.AuthScheme = provider.AuthScheme ?? string.Empty;
            client.Options.ApiFormat = provider.ApiFormat;

            LastStatusMessage = provider.Name + " · " + model + " · " + baseUrl;
        }

        /// <summary>
        /// Validação deliberadamente genérica. O catálogo define como cada provedor
        /// autentica; o RuntimeConfig não pode bloquear chaves legítimas por prefixo.
        /// Prefixos são usados somente como heurística de detecção, nunca como requisito.
        /// </summary>
        public static string? ValidateApiKeyFormat(string? key, ProviderInfo? provider)
        {
            if (provider == null || !provider.NeedsKey)
                return null;

            string k = (key ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(k))
                return null;

            if (k.Length > 4096 || k.IndexOfAny(new[] { ' ', '\t', '\r', '\n' }) >= 0)
                return "Chave de API inválida. Cole somente a chave, sem espaços ou quebras de linha.";

            if (string.IsNullOrWhiteSpace(provider.BaseUrl) && string.IsNullOrWhiteSpace(BaseUrlOverride))
                return "Este provedor exige uma BASE URL. Informe o endpoint compatível com a API.";

            if (string.IsNullOrWhiteSpace(Model) && provider.Models.Count == 0 &&
                string.IsNullOrWhiteSpace(provider.DefaultModelId))
                return "Este provedor exige um MODELO CUSTOM. Informe o ID do modelo.";

            return null;
        }

        public static string? EnsureReadyForRequest(OpenRouterClient client)
        {
            Apply(client);

            ProviderInfo provider = ProviderCatalog.Find(Provider) ?? ProviderCatalog.Providers[0];
            string key = client.Options.ApiKey ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(key))
            {
                string? fmt = ValidateApiKeyFormat(key, provider);
                if (fmt != null)
                    return fmt;
                return null;
            }

            if (!provider.NeedsKey)
                return null;

            ProviderInfo? fallback = null;
            foreach (ProviderInfo p in ProviderCatalog.Providers)
            {
                if (!p.NeedsKey)
                {
                    fallback = p;
                    break;
                }
            }

            if (fallback == null)
                return "Sem chave de API. Selecione um provedor local ou configure uma chave.";

            string previous = provider.Name;
            Provider = fallback.Id;
            if (fallback.Models.Count > 0)
                Model = string.IsNullOrWhiteSpace(fallback.DefaultModelId) ? fallback.Models[0].Id : fallback.DefaultModelId;
            Apply(client);

            LastStatusMessage = "Sem chave em " + previous + " — usando " + fallback.Name +
                                " (" + client.Options.BaseUrl + ").";
            AuraLog.Info("RuntimeConfig: " + LastStatusMessage);
            return null;
        }
    }
}
