using System.Net;
using System.Net.Http;

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

        if (s.Contains(' '))
            return "Endpoint não pode conter espaços.";

        // Formato OpenAI costuma exigir path (ex.: /api/v1/chat/completions)
        if (format == UniversalApiFormatHint.OpenAiCompatible)
        {
            var path = uri.AbsolutePath.TrimEnd('/');
            if (path.Length <= 1)
                return "URL parece incompleta: falta o caminho (ex.: /api/v1/chat/completions).";
        }

        return null;
    }

    /// <summary>
    /// Probe de rede: HEAD/GET na origem + POST leve no path de chat.
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

            using (var probe = new HttpRequestMessage(HttpMethod.Head, origin))
            {
                try
                {
                    using var ping = await http.SendAsync(probe, ct).ConfigureAwait(false);
                }
                catch (HttpRequestException)
                {
                    using var get = new HttpRequestMessage(HttpMethod.Get, origin);
                    using var ping2 = await http.SendAsync(get, ct).ConfigureAwait(false);
                }
            }

            int? status = null;
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
                var detail = status switch
                {
                    401 or 403 => "Host OK. Endpoint respondeu " + status + " (auth — key ou permissão).",
                    400 or 422 => "Host OK. Endpoint respondeu " + status + " (caminho de chat alcançável).",
                    404 => "Host OK, mas o caminho retornou 404 — confira /chat/completions ou o path do provider.",
                    405 => "Host OK. Método não permitido no path (comum) — endpoint provavelmente certo.",
                    >= 200 and < 300 => "Endpoint respondeu " + status + " — OK.",
                    >= 500 => "Host alcançável, mas o servidor retornou " + status + ".",
                    _ => "Host alcançável. HTTP " + status + " no path de chat."
                };

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
