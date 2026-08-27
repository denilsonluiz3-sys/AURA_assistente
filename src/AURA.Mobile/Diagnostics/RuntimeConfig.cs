using AURA.AI;

namespace AURA.Mobile.Diagnostics
{
    /// <summary>
    /// Configuração aplicável em tempo de execução (sem recompilar o APK).
    /// Toda alteração feita aqui persiste em Preferences e reflete
    /// imediatamente no OpenRouterClient.
    /// </summary>
    public static class RuntimeConfig
    {
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

        public static string ApiKey
        {
            get => Preferences.Default.Get("ai_api_key", string.Empty);
            set => Preferences.Default.Set("ai_api_key", value);
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

                // Modelo customizado (digitado) — aceita se não vazio
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

        /// <summary>
        /// Valida formato básico da chave para o provedor atual.
        /// null = ok; string = mensagem de erro amigável.
        /// </summary>
        public static string? ValidateApiKeyFormat(string? key, ProviderInfo? provider)
        {
            if (provider == null || !provider.NeedsKey)
                return null;

            string k = (key ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(k))
                return null; // vazio tratado em EnsureReady

            if (k.Length > 200 || k.IndexOfAny(new[] { ' ', '\t', '\r', '\n' }) >= 0)
            {
                return "Chave de API inválida (parece conter texto de log ou espaços). " +
                       "Cole só a chave, sem aspas.";
            }

            // Gemini AI Studio: AIzaSy… — tokens AQ. não são API key da Generative Language
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

        /// <summary>
        /// Garante client pronto para Chat e Agent.
        /// Sem API key: fallback para provedor NeedsKey=false (Ollama).
        /// </summary>
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
