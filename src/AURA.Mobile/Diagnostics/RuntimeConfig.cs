using AURA.AI.UniversalAI;

namespace AURA.Mobile.Diagnostics;

/// <summary>
/// Configuração de runtime da IA. A API key é a prioridade: deve gravar e voltar
/// para o cliente mesmo se SecureStorage falhar ou a config estiver incompleta.
/// </summary>
public static class RuntimeConfig
{
    private const string ApiKeyPrefix = "ai_api_key_";
    private const string LegacyApiKeyPref = "ai_api_key";
    private const string LegacyApiKeySecure = "ai_api_key";

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
        set => Preferences.Default.Set("ai_base_url", EndpointValidator.Normalize(value));
    }

    public static string ModelsUrlOverride
    {
        get => Preferences.Default.Get("ai_models_url", string.Empty);
        set => Preferences.Default.Set("ai_models_url", EndpointValidator.Normalize(value));
    }

    public static UniversalApiFormat ApiFormat
    {
        get => Enum.TryParse<UniversalApiFormat>(Preferences.Default.Get("ai_api_format", string.Empty), true, out var f)
            ? f
            : UniversalApiFormat.OpenAiCompatible;
        set => Preferences.Default.Set("ai_api_format", value.ToString());
    }

    public static string AuthHeader
    {
        get => Preferences.Default.Get("ai_auth_header", "Authorization");
        set => Preferences.Default.Set("ai_auth_header", string.IsNullOrWhiteSpace(value) ? "Authorization" : value.Trim());
    }

    public static string AuthScheme
    {
        get => Preferences.Default.Get("ai_auth_scheme", "Bearer");
        set => Preferences.Default.Set("ai_auth_scheme", value?.Trim() ?? "Bearer");
    }

    public static bool RequiresApiKey
    {
        get => Preferences.Default.Get("ai_requires_key", true);
        set => Preferences.Default.Set("ai_requires_key", value);
    }

    public static string LastStatusMessage { get; private set; } = string.Empty;

    public static string ApiKey
    {
        get => GetApiKeyForProvider(Provider);
        set => SetApiKeyForProvider(Provider, value);
    }

    public static string GetApiKeyForProvider(string? providerId)
    {
        var id = Normalize(providerId);

        if (!string.IsNullOrEmpty(id))
        {
            try
            {
                var fromPref = Preferences.Default.Get(ApiKeyPrefix + id, string.Empty)?.Trim();
                if (!string.IsNullOrEmpty(fromPref))
                    return ApiKeyValidator.Normalize(fromPref);
            }
            catch { /* ignore */ }
        }

        if (!string.IsNullOrEmpty(id))
        {
            var fromSecure = ReadSecure(ApiKeyPrefix + id);
            if (!string.IsNullOrEmpty(fromSecure))
            {
                var norm = ApiKeyValidator.Normalize(fromSecure);
                try { Preferences.Default.Set(ApiKeyPrefix + id, norm); } catch { /* ignore */ }
                return norm;
            }
        }

        try
        {
            var legacy = Preferences.Default.Get(LegacyApiKeyPref, string.Empty)?.Trim();
            if (!string.IsNullOrEmpty(legacy))
                return ApiKeyValidator.Normalize(legacy);
        }
        catch { /* ignore */ }

        var legacySecure = ReadSecure(LegacyApiKeySecure);
        if (!string.IsNullOrEmpty(legacySecure))
            return ApiKeyValidator.Normalize(legacySecure);

        return string.Empty;
    }

    public static void SetApiKeyForProvider(string? providerId, string? value)
    {
        var id = Normalize(providerId);
        var v = ApiKeyValidator.Normalize(value);

        if (string.IsNullOrEmpty(id))
        {
            try
            {
                if (v.Length == 0)
                    Preferences.Default.Remove(LegacyApiKeyPref);
                else
                    Preferences.Default.Set(LegacyApiKeyPref, v);
            }
            catch (Exception ex)
            {
                AuraLog.Exception("RuntimeConfig.SetApiKey.legacyPref", ex);
            }

            WriteSecure(LegacyApiKeySecure, v);
            return;
        }

        var prefKey = ApiKeyPrefix + id;
        try
        {
            if (v.Length == 0)
                Preferences.Default.Remove(prefKey);
            else
                Preferences.Default.Set(prefKey, v);
        }
        catch (Exception ex)
        {
            AuraLog.Exception("RuntimeConfig.SetApiKey.pref", ex);
        }

        try
        {
            if (v.Length == 0)
                Preferences.Default.Remove(LegacyApiKeyPref);
            else
                Preferences.Default.Set(LegacyApiKeyPref, v);
        }
        catch { /* ignore */ }

        WriteSecure(prefKey, v);
        WriteSecure(LegacyApiKeySecure, v);
    }

    public static void ClearApiKey() => SetApiKeyForProvider(Provider, string.Empty);

    public static string NormalizeChatBaseUrl(string? url, string? providerId = null)
        => EndpointValidator.Normalize(url);

    public static string? ValidateCurrentEndpoint()
    {
        var hint = ApiFormat switch
        {
            UniversalApiFormat.AnthropicMessages => UniversalApiFormatHint.AnthropicMessages,
            UniversalApiFormat.Gemini => UniversalApiFormatHint.Gemini,
            _ => UniversalApiFormatHint.OpenAiCompatible
        };
        return EndpointValidator.ValidateFormat(BaseUrlOverride, hint);
    }

    public static UniversalConnection CreateConnection()
    {
        var provider = Provider.Trim();
        var model = Model.Trim();
        var baseUrl = NormalizeChatBaseUrl(BaseUrlOverride);
        if (string.IsNullOrWhiteSpace(provider))
            throw new InvalidOperationException("Provider não configurado.");
        if (string.IsNullOrWhiteSpace(model))
            throw new InvalidOperationException("Modelo não configurado.");
        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new InvalidOperationException("Endpoint não configurado.");

        var endpointError = ValidateCurrentEndpoint();
        if (endpointError != null)
            throw new InvalidOperationException(endpointError);

        var apiKey = GetApiKeyForProvider(provider);
        return UniversalRuntimeAdapter.CreateConnection(
            provider, apiKey, model, baseUrl, ModelsUrlOverride,
            ApiFormat, AuthHeader, AuthScheme, RequiresApiKey);
    }

    public static void Apply(IUniversalAiClient client)
    {
        ArgumentNullException.ThrowIfNull(client);
        ApplyPartial(client);

        try
        {
            var c = CreateConnection();
            var configured = UniversalAiClientFactory.Create(c, MaxTokens, TimeoutSeconds);
            client.Options.Provider = configured.Options.Provider;
            client.Options.ApiKey = configured.Options.ApiKey;
            client.Options.BaseUrl = configured.Options.BaseUrl;
            client.Options.Model = configured.Options.Model;
            client.Options.MaxTokens = configured.Options.MaxTokens;
            client.Options.TimeoutSeconds = configured.Options.TimeoutSeconds;
            client.Options.AuthHeaderName = configured.Options.AuthHeaderName;
            client.Options.AuthScheme = configured.Options.AuthScheme;
            client.Options.ApiFormat = configured.Options.ApiFormat;
            LastStatusMessage = c.Provider.Name + " · " + c.Model + " · " + c.Provider.BaseUrl;
        }
        catch (Exception ex)
        {
            LastStatusMessage = "Config parcial: " + ex.Message;
            AuraLog.Info("RuntimeConfig.Apply partial: " + ex.Message);
        }
    }

    public static void ApplyPartial(IUniversalAiClient client)
    {
        ArgumentNullException.ThrowIfNull(client);
        var provider = Provider.Trim();
        client.Options.Provider = provider;
        client.Options.ApiKey = GetApiKeyForProvider(provider);
        client.Options.BaseUrl = NormalizeChatBaseUrl(BaseUrlOverride);
        client.Options.Model = Model.Trim();
        client.Options.MaxTokens = Math.Max(1, MaxTokens);
        client.Options.TimeoutSeconds = Math.Max(1, TimeoutSeconds);
        client.Options.AuthHeaderName = string.IsNullOrWhiteSpace(AuthHeader) ? "Authorization" : AuthHeader;
        client.Options.AuthScheme = AuthScheme ?? "Bearer";
        client.Options.ApiFormat = ApiFormat;
    }

    public static string? EnsureReadyForRequest(IUniversalAiClient client)
    {
        if (client == null)
            return "Cliente de IA não configurado.";

        ApplyPartial(client);

        if (string.IsNullOrWhiteSpace(client.Options.BaseUrl))
            return "Configure o endpoint do provider (⚙).";

        var endpointError = EndpointValidator.ValidateFormat(
            client.Options.BaseUrl,
            client.Options.ApiFormat switch
            {
                UniversalApiFormat.AnthropicMessages => UniversalApiFormatHint.AnthropicMessages,
                UniversalApiFormat.Gemini => UniversalApiFormatHint.Gemini,
                _ => UniversalApiFormatHint.OpenAiCompatible
            });
        if (endpointError != null)
            return endpointError;

        if (string.IsNullOrWhiteSpace(client.Options.Model))
            return "Configure o modelo (⚙).";

        if (RequiresApiKey)
        {
            var keyResult = ApiKeyValidator.ValidateFormat(
                client.Options.ApiKey, client.Options.Provider, required: true);
            if (!keyResult.Success)
                return keyResult.Message;
            // Avisos de prefixo não bloqueiam a chamada
        }

        return null;
    }

    public static IUniversalAiClient CreateClient()
    {
        var c = CreateConnection();
        return UniversalAiClientFactory.Create(c, MaxTokens, TimeoutSeconds);
    }

    private static string Normalize(string? value)
    {
        var s = (value ?? string.Empty).Trim().ToLowerInvariant();
        if (s.Length == 0)
            return string.Empty;
        return new string(s.Select(c => char.IsLetterOrDigit(c) || c is '-' or '_' ? c : '_').ToArray());
    }

    private static string ReadSecure(string key)
    {
        try
        {
            return Task.Run(async () =>
            {
                try
                {
                    return (await SecureStorage.Default.GetAsync(key).ConfigureAwait(false))?.Trim() ?? string.Empty;
                }
                catch
                {
                    return string.Empty;
                }
            }).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            AuraLog.Info("RuntimeConfig.ReadSecure falhou: " + ex.Message);
            return string.Empty;
        }
    }

    private static void WriteSecure(string key, string value)
    {
        try
        {
            Task.Run(async () =>
            {
                try
                {
                    if (string.IsNullOrEmpty(value))
                        SecureStorage.Default.Remove(key);
                    else
                        await SecureStorage.Default.SetAsync(key, value).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    AuraLog.Info("RuntimeConfig.WriteSecure falhou: " + ex.Message);
                }
            }).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            AuraLog.Info("RuntimeConfig.WriteSecure outer: " + ex.Message);
        }
    }
}
