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
            }

            if (!modelBelongsToProvider && provider.Models.Count > 0)
            {
                model = provider.Models[0].Id;
            }

            client.Options.Provider = provider.Id;
            client.Options.BaseUrl = provider.BaseUrl;
            client.Options.Model = model;
            client.Options.MaxTokens = MaxTokens;
            client.Options.TimeoutSeconds = TimeoutSeconds;
            client.Options.ApiKey = ApiKey;
            client.Options.AuthHeaderName = provider.AuthHeaderName;
            client.Options.AuthScheme = provider.AuthScheme;
            client.Options.ApiFormat = provider.ApiFormat;
        }

        /// <summary>
        /// Garante client pronto para Chat e Agent.
        /// Sem API key: se o provedor exige chave, faz fallback automático para o
        /// primeiro provedor com NeedsKey=false (ex.: Ollama local) — mesma regra
        /// para as duas abas.
        /// </summary>
        /// <returns>null se ok; mensagem de erro amigável se impossível continuar.</returns>
        public static string? EnsureReadyForRequest(OpenRouterClient client)
        {
            Apply(client);

            ProviderInfo provider = ProviderCatalog.Find(Provider) ?? ProviderCatalog.Providers[0];
            string key = client.Options.ApiKey ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(key))
            {
                if (key.Length > 200 || key.IndexOfAny(new[] { ' ', '\t', '\r', '\n' }) >= 0)
                {
                    return "Chave de API inválida (parece conter texto de log). " +
                           "Toque em 'Restaurar padrão' na aba Correções e digite a chave manualmente.";
                }
                return null;
            }

            if (!provider.NeedsKey)
            {
                return null;
            }

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
                return "Sem chave de API. Configure uma chave no painel da IA, " +
                       "ou use um provedor local (Ollama) que não exige chave.";
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
