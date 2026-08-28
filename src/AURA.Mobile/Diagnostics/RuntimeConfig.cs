using AURA.AI;

namespace AURA.Mobile.Diagnostics
{
    /// <summary>
    /// Configuração aplicável em tempo de execução (sem recompilar o APK).
    /// Preferências não sensíveis usam Preferences; a API key usa SecureStorage
    /// (Keystore / armazenamento cifrado no Android).
    /// </summary>
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

        /// <summary>Id do provedor (ex.: gemini, openrouter) — não o display name.</summary>
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

        /// <summary>
        /// Chave de API: SecureStorage. Migra automaticamente de Preferences legadas.
        /// </summary>
        public static string ApiKey
        {
            get
            {
                try
                {
                    string? secure = SecureStorage.Default
                        .GetAsync(ApiKeySecureName)
                        .GetAwaiter()
                        .GetResult();

                    if (!string.IsNullOrWhiteSpace(secure))
                        return secure.Trim();
                }
                catch
                {
                    // SecureStorage indisponível ou valor de backup ilegível
                }

                string legacy = Preferences.Default.Get(ApiKeyLegacyPref, string.Empty);
                if (string.IsNullOrWhiteSpace(legacy))
                    return string.Empty;

                // Migração única: sobe para SecureStorage e apaga texto claro
                try
                {
                    SecureStorage.Default
                        .SetAsync(ApiKeySecureName, legacy.Trim())
                        .GetAwaiter()
                        .GetResult();
                    Preferences.Default.Remove(ApiKeyLegacyPref);
                }
                catch
                {
                    // Mantém leitura legada se SecureStorage falhar
                }

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
                        SecureStorage.Default
                            .SetAsync(ApiKeySecureName, v)
                            .GetAwaiter()
                            .GetResult();
                }
                catch
                {
                    // Fallback mínimo: não gravar de novo em Preferences em claro
                }

                // Nunca deixar cópia legada
                try { Preferences.Default.Remove(ApiKeyLegacyPref); }
                catch { }
            }
        }

        /// <summary>Remove a key do SecureStorage e de Preferences legadas.</summary>
        public static void ClearApiKey()
        {
            ApiKey = string.Empty;
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
                    if (string.Equals(m.Id, model, System.StringComparison.OrdinalIgnoreCase))
                    {
                        modelBelongsToProvider = true;
                        break;
                    }
                }

                if (!modelBelongsToProvider && model.Length >= 2)
                    modelBelongsToProvider = true;
            }

            if (!modelBelongsToProvider && provider.Models.Count > 0)
            {
                model = !string.IsNullOrWhiteSpace(provider.DefaultModelId)
                    ? provider.DefaultModelId
                    : provider.Models[0].Id;
            }

            client.Options.Provider = provider.Id;
            client.Options.BaseUrl = provider.BaseUrl;
            client.Options.Model = model;
            client.Options.MaxTokens = MaxTokens;
            client.Options.TimeoutSeconds = TimeoutSeconds;
            client.Options.ApiKey = ApiKey?.Trim() ?? string.Empty;
            client.Options.AuthHeaderName = provider.AuthHeaderName;
            client.Options.AuthScheme = provider.AuthScheme;
            client.Options.ApiFormat = provider.ApiFormat;
        }

        public static string? ValidateApiKeyFormat(string? key, ProviderInfo? provider)
        {
            if (provider == null || !provider.NeedsKey)
                return null;

            string k = (key ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(k))
                return null;

            if (k.Length > 200 || k.IndexOfAny(new[] { ' ', '\t', '\r', '\n' }) >= 0)
            {
                return "Chave de API inválida (parece conter texto de log ou espaços). " +
                       "Cole só a chave, sem aspas.";
            }

            if (string.Equals(provider.Id, "gemini", System.StringComparison.OrdinalIgnoreCase))
            {
                if (k.StartsWith("AQ.", System.StringComparison.Ordinal))
                {
                    return "Esta chave começa com AQ. e não é uma API key do Google AI Studio. " +
                           "Em aistudio.google.com/apikey crie uma chave que começa com AIzaSy…";
                }

                if (!k.StartsWith("AIza", System.StringComparison.Ordinal))
                {
                    return "Chave Gemini costuma começar com AIzaSy…. " +
                           "Confira em Google AI Studio → Get API key.";
                }
            }

            if (string.Equals(provider.Id, "openrouter", System.StringComparison.OrdinalIgnoreCase) &&
                !k.StartsWith("sk-or-", System.StringComparison.Ordinal))
            {
                return "OpenRouter espera chave sk-or-…. Se a chave for de outro provedor, troque o seletor.";
            }

            if (string.Equals(provider.Id, "groq", System.StringComparison.OrdinalIgnoreCase) &&
                !k.StartsWith("gsk_", System.StringComparison.Ordinal))
            {
                return "Groq espera chave gsk_….";
            }

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
            {
                return "Sem chave de API. Configure uma chave no painel ⚙ da IA, " +
                       "use OpenRouter/Groq/Gemini, ou Ollama local (sem chave).";
            }

            Provider = fallback.Id;
            if (fallback.Models.Count > 0)
            {
                Model = string.IsNullOrWhiteSpace(fallback.DefaultModelId)
                    ? fallback.Models[0].Id
                    : fallback.DefaultModelId;
            }
            Apply(client);
            AuraLog.Info("RuntimeConfig: sem API key — fallback para provedor '" + fallback.Id + "' (NeedsKey=false).");
            return null;
        }
    }
}
