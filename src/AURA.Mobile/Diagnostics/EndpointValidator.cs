using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace AURA.Mobile.Diagnostics;

/// <summary>
/// Validação de endpoint de chat: formato (síncrono) e alcance de rede (async).
/// </summary>
public static class EndpointValidator
{
    /// <summary>
    /// Normaliza URL de chat: trim, remove barra final, acrescenta https:// se faltar esquema.
    /// </summary>
    public static string Normalize(string? url)
    {
        var s = (url ?? string.Empty).Trim();
        if (s.Length == 0)
            return string.Empty;

        // Usuário colou host sem esquema (ex.: openrouter.ai/api/v1/chat/completions)
        if (!s.Contains("://", StringComparison.Ordinal))
            s = "https://" + s.TrimStart('/');

        return s.TrimEnd('/');
    }

    /// <summary>
    /// Validação estática. Retorna null se OK, ou mensagem de erro em português.
    /// </summary>
    public static string? ValidateFormat(string? url, UniversalApiFormatHint format = UniversalApiFormatHint.OpenAiCompatible)
    {
        var s = Normalize(url);
        if (string.IsNullOrWhiteSpace(s))
            return "Endpoint vazio. Informe a URL completa do chat (ex.: https://…/chat/completions).";

        if (!Uri.TryCreate(s, UriKind.Absolute, out var uri))
            return "Endpoint inválido: não é uma URL absoluta.";

        if (uri.Scheme is not ("http" or "https"))
            return "Endpoint deve usar http:// ou https://.";

        if (string.IsNullOrWhiteSpace(uri.Host))
            return "Endpoint sem host.";

        // localhost / IPs privados são válidos (Ollama no aparelho ou LAN)
        if (uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
            || uri.Host == "127.0.0.1"
            || uri.Host == "[::1]")
        {
            // ok
        }

        if (s.Contains(' '))
            return "Endpoint não pode conter espaços.";

        // Avisos leves (não bloqueiam): formato OpenAI costuma terminar em chat/completions
        if (format == UniversalApiFormatHint.OpenAiCompatible)
        {
            var path = uri.AbsolutePath.TrimEnd('/');
            if (path.Length <= 1)
                return "URL parece incompleta: falta o caminho (ex.: /api/v1/chat/completions).";
        }

        return null;
    }

    /// <summary>
    /// Probe de rede: HEAD (ou GET) na origem do endpoint. Não envia body nem gasta tokens.
    /// </summary>
    public static async Task<EndpointProbeResult> ProbeAsync(
        string? url,
        string? apiKey = null,
        string authHeader = "Authorization",
        string authScheme = "Bearer",
        int timeoutSeconds = 15,
        CancellationToken ct = default)
    {
        var formatError = ValidateFormat(url);
        if (formatError != null)
            return EndpointProbeResult.Fail(formatError, reachedNetwork: false);

        var normalized = Normalize(url);
        var uri = new Uri(normalized);
        var origin = uri.GetLeftPart(UriPartial.Authority);

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

            // 1) Alcance do host
            using (var probe = new HttpRequestMessage(HttpMethod.Head, origin))
            {
                try
                {
                    using var ping = await http.SendAsync(probe, ct).ConfigureAwait(false);
                    // Qualquer resposta HTTP conta como host alcançável
                }
                catch (HttpRequestException)
                {
                    // Alguns hosts rejeitam HEAD — tentar GET leve na origem
                    using var get = new HttpRequestMessage(HttpMethod.Get, origin);
                    using var ping2 = await http.SendAsync(get, ct).ConfigureAwait(false);
                }
            }

            // 2) Opcional: POST vazio não — só confirma que a URL de chat responde algo ≠ DNS fail
            // OPTIONS ou GET no path completo pode 404/405; isso ainda é “endpoint alcançável”
            int? status = null;
            string detail;
            try
            {
                using var pathReq = new HttpRequestMessage(HttpMethod.Post, normalized);
                pathReq.Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");
                if (!string.IsNullOrWhiteSpace(apiKey))
                {
                    var header = string.IsNullOrWhiteSpace(authHeader) ? "Authorization" : authHeader;
                    var scheme = authScheme?.Trim() ?? string.Empty;
                    pathReq.Headers.TryAddWithoutValidation(
                        header,
                        string.IsNullOrEmpty(scheme) ? apiKey.Trim() : scheme + " " + apiKey.Trim());
                }

                using var pathResp = await http.SendAsync(pathReq, ct).ConfigureAwait(false);
                status = (int)pathResp.StatusCode;
                detail = status switch
                {
                    401 or 403 => "Host OK. Endpoint respondeu " + status + " (auth — key ou permissão).",
                    400 or 422 => "Host OK. Endpoint respondeu " + status + " (caminho de chat alcançável).",
                    404 => "Host OK, mas o caminho retornou 404 — confira /chat/completions ou o path do provider.",
                    405 => "Host OK. Método não permitido no path (comum) — endpoint provavelmente certo.",
                    >= 200 and < 300 => "Endpoint respondeu " + status + " — OK.",
                    >= 500 => "Host alcançável, mas o servidor retornou " + status + ".",
                    _ => "Host alcançável. HTTP " + status + " no path de chat."
                };

                // 404 no path é aviso, não falha dura de rede
                if (status == 404)
                    return EndpointProbeResult.Warn(detail, status);

                return EndpointProbeResult.Ok(detail, status);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return EndpointProbeResult.Ok(
                    "Host alcançável; path de chat não testado (“" + ex.Message + "”).",
                    status);
            }
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return EndpointProbeResult.Fail("Tempo esgotado ao contatar o endpoint.", reachedNetwork: false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (HttpRequestException ex)
        {
            return EndpointProbeResult.Fail("Sem rede ou host inacessível: " + ex.Message, reachedNetwork: false);
        }
        catch (Exception ex)
        {
            return EndpointProbeResult.Fail("Falha ao validar endpoint: " + ex.Message, reachedNetwork: false);
        }
    }
}

public enum UniversalApiFormatHint
{
    OpenAiCompatible,
    AnthropicMessages,
    Gemini,
    Other
}

public sealed class EndpointProbeResult
{
    public bool Success { get; init; }
    public bool IsWarning { get; init; }
    public bool ReachedNetwork { get; init; }
    public string Message { get; init; } = string.Empty;
    public int? HttpStatus { get; init; }

    public static EndpointProbeResult Ok(string message, int? status = null) => new()
    {
        Success = true,
        IsWarning = false,
        ReachedNetwork = true,
        Message = message,
        HttpStatus = status
    };

    public static EndpointProbeResult Warn(string message, int? status = null) => new()
    {
        Success = true,
        IsWarning = true,
        ReachedNetwork = true,
        Message = message,
        HttpStatus = status
    };

    public static EndpointProbeResult Fail(string message, bool reachedNetwork) => new()
    {
        Success = false,
        IsWarning = false,
        ReachedNetwork = reachedNetwork,
        Message = message
    };
}
