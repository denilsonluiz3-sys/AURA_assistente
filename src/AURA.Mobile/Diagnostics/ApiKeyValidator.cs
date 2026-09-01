using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using AURA.AI.UniversalAI;

namespace AURA.Mobile.Diagnostics;

/// <summary>
/// Validação de API key: formato (síncrono) e verificação ao vivo no endpoint de chat.
/// Agnóstico a provider — prefixos conhecidos só geram aviso, não bloqueio.
/// </summary>
public static class ApiKeyValidator
{
    /// <summary>Remove espaços e prefixo "Bearer " colado por engano.</summary>
    public static string Normalize(string? apiKey)
    {
        var s = (apiKey ?? string.Empty).Trim();
        if (s.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            s = s["Bearer ".Length..].Trim();
        return s;
    }

    /// <summary>
    /// Validação estática. null = OK. Soft warnings via ApiKeyValidationResult.Warning.
    /// </summary>
    public static ApiKeyValidationResult ValidateFormat(
        string? apiKey,
        string? providerId = null,
        bool required = true)
    {
        var key = Normalize(apiKey);

        if (string.IsNullOrEmpty(key))
        {
            if (required)
                return ApiKeyValidationResult.Fail("API key vazia. Cole a chave do provider (⚙).");
            return ApiKeyValidationResult.Ok("Sem API key (provider local / sem autenticação).");
        }

        if (key.Contains(' ') || key.Contains('\n') || key.Contains('\t'))
            return ApiKeyValidationResult.Fail("API key não pode conter espaços ou quebras de linha.");

        if (key.Length < 8)
            return ApiKeyValidationResult.Fail("API key muito curta (mínimo 8 caracteres).");

        if (key.Length > 512)
            return ApiKeyValidationResult.Fail("API key parece inválida (mais de 512 caracteres).");

        // Avisos por família de provider (não bloqueiam)
        var provider = (providerId ?? string.Empty).Trim().ToLowerInvariant();
        string? warn = null;

        if (provider.Contains("openrouter") && !key.StartsWith("sk-or-", StringComparison.Ordinal))
            warn = "OpenRouter costuma usar chaves que começam com sk-or-. Confira se colou a key certa.";
        else if ((provider.Contains("openai") || provider == "gpt") && !key.StartsWith("sk-", StringComparison.Ordinal))
            warn = "OpenAI costuma usar chaves que começam com sk-.";
        else if (provider.Contains("anthropic") || provider.Contains("claude"))
        {
            if (!key.StartsWith("sk-ant-", StringComparison.Ordinal))
                warn = "Anthropic costuma usar chaves que começam com sk-ant-.";
        }
        else if ((provider.Contains("google") || provider.Contains("gemini")) && !key.StartsWith("AIza", StringComparison.Ordinal))
            warn = "Google AI costuma usar chaves que começam com AIza.";
        else if (provider.Contains("deepseek") && !key.StartsWith("sk-", StringComparison.Ordinal))
            warn = "DeepSeek costuma usar chaves que começam com sk-.";

        if (warn != null)
            return ApiKeyValidationResult.Warn(warn, key);

        return ApiKeyValidationResult.Ok("Formato da API key OK.", key);
    }

    /// <summary>
    /// Teste ao vivo: POST mínimo no endpoint de chat (max_tokens=1).
    /// Interpreta 401/403 como key inválida; 200/400 com body de modelo como key aceita.
    /// </summary>
    public static async Task<ApiKeyValidationResult> VerifyLiveAsync(
        string? apiKey,
        string? endpoint,
        string? model,
        UniversalApiFormat format = UniversalApiFormat.OpenAiCompatible,
        string authHeader = "Authorization",
        string authScheme = "Bearer",
        string? providerId = null,
        int timeoutSeconds = 20,
        CancellationToken ct = default)
    {
        var formatResult = ValidateFormat(apiKey, providerId, required: true);
        if (!formatResult.Success)
            return formatResult;

        var key = formatResult.NormalizedKey ?? Normalize(apiKey);
        var url = EndpointValidator.Normalize(endpoint);
        var endpointError = EndpointValidator.ValidateFormat(url);
        if (endpointError != null)
            return ApiKeyValidationResult.Fail("Endpoint inválido antes de testar a key: " + endpointError);

        var modelId = (model ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(modelId))
            return ApiKeyValidationResult.Fail("Informe o modelo para testar a API key no endpoint.");

        try
        {
            using var handler = new HttpClientHandler
            {
                AllowAutoRedirect = true,
                AutomaticDecompression = DecompressionMethods.All
            };
            using var http = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(Math.Clamp(timeoutSeconds, 5, 60))
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            var header = string.IsNullOrWhiteSpace(authHeader) ? "Authorization" : authHeader;
            var scheme = authScheme?.Trim() ?? string.Empty;
            request.Headers.TryAddWithoutValidation(
                header,
                string.IsNullOrEmpty(scheme) ? key : scheme + " " + key);

            // Anthropic exige header extra
            if (format == UniversalApiFormat.AnthropicMessages)
                request.Headers.TryAddWithoutValidation("anthropic-version", "2023-06-01");

            request.Content = new StringContent(
                BuildMinimalPayload(format, modelId),
                Encoding.UTF8,
                "application/json");

            using var response = await http.SendAsync(request, ct).ConfigureAwait(false);
            var status = (int)response.StatusCode;
            var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            var bodyShort = body.Length > 240 ? body[..240] + "…" : body;

            return status switch
            {
                200 or 201 => ApiKeyValidationResult.Ok(
                    "API key aceita pelo provider (HTTP " + status + ").", key),

                401 => ApiKeyValidationResult.Fail(
                    "API key rejeitada (401). Confira a chave ou o header de autenticação."),

                403 => ApiKeyValidationResult.Fail(
                    "Acesso negado (403). Key sem permissão ou plano insuficiente."),

                402 => ApiKeyValidationResult.Fail(
                    "Sem créditos / pagamento necessário (402). Key reconhecida, conta sem saldo."),

                429 => ApiKeyValidationResult.Warn(
                    "Rate limit (429). A key parece válida, mas o provider limitou requisições.", key),

                400 or 422 =>
                    // Muitos providers: key OK mas payload mínimo incompleto
                    LooksLikeAuthError(body)
                        ? ApiKeyValidationResult.Fail("Provider indicou erro de autenticação no body (HTTP " + status + ").")
                        : ApiKeyValidationResult.Ok(
                            "API key provavelmente válida (HTTP " + status + " no payload de teste).", key),

                404 => ApiKeyValidationResult.Fail(
                    "Endpoint 404 ao testar a key — confira a URL de chat."),

                >= 500 => ApiKeyValidationResult.Warn(
                    "Servidor do provider falhou (" + status + "). Key não confirmada. " + bodyShort, key),

                _ => ApiKeyValidationResult.Warn(
                    "HTTP " + status + " ao testar key. " + bodyShort, key)
            };
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return ApiKeyValidationResult.Fail("Tempo esgotado ao verificar a API key.");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (HttpRequestException ex)
        {
            return ApiKeyValidationResult.Fail("Rede ao verificar key: " + ex.Message);
        }
        catch (Exception ex)
        {
            return ApiKeyValidationResult.Fail("Falha ao verificar key: " + ex.Message);
        }
    }

    private static string BuildMinimalPayload(UniversalApiFormat format, string model)
    {
        return format switch
        {
            UniversalApiFormat.AnthropicMessages => JsonSerializer.Serialize(new
            {
                model,
                max_tokens = 1,
                messages = new[] { new { role = "user", content = "ping" } }
            }),
            UniversalApiFormat.Gemini => JsonSerializer.Serialize(new
            {
                contents = new[]
                {
                    new { role = "user", parts = new[] { new { text = "ping" } } }
                },
                generationConfig = new { maxOutputTokens = 1 }
            }),
            _ => JsonSerializer.Serialize(new
            {
                model,
                max_tokens = 1,
                messages = new[] { new { role = "user", content = "ping" } }
            })
        };
    }

    private static bool LooksLikeAuthError(string body)
    {
        if (string.IsNullOrEmpty(body))
            return false;
        var b = body.ToLowerInvariant();
        return b.Contains("invalid api key")
            || b.Contains("invalid_api_key")
            || b.Contains("authentication")
            || b.Contains("unauthorized")
            || b.Contains("invalid token")
            || b.Contains("incorrect api key");
    }
}

public sealed class ApiKeyValidationResult
{
    public bool Success { get; init; }
    public bool IsWarning { get; init; }
    public string Message { get; init; } = string.Empty;
    public string? NormalizedKey { get; init; }

    public static ApiKeyValidationResult Ok(string message, string? key = null) => new()
    {
        Success = true,
        IsWarning = false,
        Message = message,
        NormalizedKey = key
    };

    public static ApiKeyValidationResult Warn(string message, string? key = null) => new()
    {
        Success = true,
        IsWarning = true,
        Message = message,
        NormalizedKey = key
    };

    public static ApiKeyValidationResult Fail(string message) => new()
    {
        Success = false,
        IsWarning = false,
        Message = message
    };
}
