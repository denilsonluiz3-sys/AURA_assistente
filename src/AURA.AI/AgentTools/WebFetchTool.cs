using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AURA.Core.Security;

namespace AURA.AI
{
    /// <summary>Busca texto HTTP público sem executar JavaScript.</summary>
    public sealed class WebFetchTool : AgentTool
    {
        private const int MaxChars = 80_000;
        private const long MaxBytes = 4L * 1024 * 1024;
        private static readonly HttpClient Http = CreateClient();

        private static HttpClient CreateClient()
        {
            var handler = new HttpClientHandler { AllowAutoRedirect = false };
            var c = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) };
            c.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "AURA-Agent/1.0 (Android; web_fetch)");
            c.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "text/html, application/json, text/plain;q=0.9");
            return c;
        }

        public override AgentToolDefinition Definition => new AgentToolDefinition
        {
            Name = "web_fetch",
            Description = "Baixa texto, HTML ou JSON de uma URL HTTP pública. Não executa JavaScript.",
            Parameters = { ["url"] = new AgentToolParameter { Type = "string", Description = "URL absoluta HTTP(S) pública" } },
            Required = { "url" }
        };

        public override async Task<string> ExecuteAsync(string argumentsJson, CancellationToken ct = default)
        {
            string url;
            using (JsonDocument doc = JsonDocument.Parse(argumentsJson))
                url = (ReadString(doc.RootElement, "url") ?? string.Empty).Trim();

            if (!WebSecurityPolicy.TryValidateHttpUrl(url, out Uri current))
                return "ERRO: URL HTTP(S) inválida.";

            try
            {
                for (int redirect = 0; redirect <= WebSecurityPolicy.MaxRedirects; redirect++)
                {
                    if (!await WebSecurityPolicy.IsPublicHttpUrlAsync(current.AbsoluteUri, ct).ConfigureAwait(false))
                        return "ERRO: destino não público ou não permitido.";

                    using var req = new HttpRequestMessage(HttpMethod.Get, current);
                    using HttpResponseMessage response = await Http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);

                    if ((int)response.StatusCode is >= 300 and <= 399)
                    {
                        Uri? location = response.Headers.Location;
                        if (location == null || !WebSecurityPolicy.TryValidateHttpUrl(new Uri(current, location).AbsoluteUri, out current))
                            return "ERRO: redirecionamento para destino inválido.";
                        continue;
                    }

                    int code = (int)response.StatusCode;
                    string media = response.Content.Headers.ContentType?.MediaType ?? "?";
                    if (!response.IsSuccessStatusCode)
                    {
                        string errBody = await ReadLimitedAsync(response, 2000, ct).ConfigureAwait(false);
                        return $"ERRO: HTTP {code} ({media})\n{errBody}";
                    }

                    long? length = response.Content.Headers.ContentLength;
                    if (length.HasValue && length.Value > MaxBytes)
                        return "ERRO: resposta excede o limite permitido.";

                    string body = await ReadLimitedAsync(response, MaxChars, ct).ConfigureAwait(false);
                    var sb = new StringBuilder();
                    sb.Append("HTTP ").Append(code).Append(" · ").Append(media);
                    if (body.Length >= MaxChars) sb.Append(" · truncado a ").Append(MaxChars).Append(" chars");
                    sb.Append('\n').Append(body);
                    return sb.ToString();
                }

                return "ERRO: número máximo de redirecionamentos excedido.";
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                return "ERRO: operação cancelada.";
            }
            catch (TaskCanceledException)
            {
                return "ERRO: tempo limite (30s) ao buscar URL.";
            }
            catch (Exception ex)
            {
                return "ERRO: falha ao buscar URL (" + ex.GetType().Name + ").";
            }
        }

        private static async Task<string> ReadLimitedAsync(HttpResponseMessage response, int maxChars, CancellationToken ct)
        {
            var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            using var reader = new System.IO.StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            var buf = new char[Math.Min(8192, maxChars)];
            var sb = new StringBuilder(Math.Min(maxChars, 4096));
            while (sb.Length < maxChars)
            {
                int n = await reader.ReadAsync(buf.AsMemory(0, Math.Min(buf.Length, maxChars - sb.Length)), ct).ConfigureAwait(false);
                if (n <= 0) break;
                sb.Append(buf, 0, n);
            }
            return sb.ToString();
        }
    }
}
