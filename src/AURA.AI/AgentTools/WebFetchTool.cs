using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AURA.AI
{
    /// <summary>
    /// HTTP GET de URL pública. Não usa UniversalAI (isso é chat/completions).
    /// UniversalAI resume/analisa o texto depois, no loop do agente.
    /// </summary>
    public sealed class WebFetchTool : AgentTool
    {
        private const int MaxChars = 80_000;

        private static readonly HttpClient Http = CreateClient();

        private static HttpClient CreateClient()
        {
            var c = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            c.DefaultRequestHeaders.TryAddWithoutValidation(
                "User-Agent", "AURA-Agent/1.0 (Android; web_fetch)");
            c.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "*/*");
            return c;
        }

        public override AgentToolDefinition Definition => new AgentToolDefinition
        {
            Name = "web_fetch",
            Description =
                "Baixa o corpo de uma URL via HTTP GET (HTML, JSON, texto). " +
                "Use para APIs e páginas simples. Não executa JavaScript. " +
                "Não é chat de IA — para modelo use a conversa do agente (UniversalAI). " +
                "Para abrir página na UI use open_browser.",
            Parameters =
            {
                ["url"] = new AgentToolParameter
                {
                    Type = "string",
                    Description = "URL absoluta http:// ou https://"
                }
            },
            Required = { "url" }
        };

        public override async Task<string> ExecuteAsync(string argumentsJson, CancellationToken ct = default)
        {
            string url;
            using (JsonDocument doc = JsonDocument.Parse(argumentsJson))
                url = ReadString(doc.RootElement, "url") ?? string.Empty;

            url = (url ?? string.Empty).Trim();
            if (url.Length == 0)
                return "ERRO: URL não fornecida.";

            if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return "ERRO: URL deve começar com http:// ou https://";

            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Get, url);
                using var response = await Http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct)
                    .ConfigureAwait(false);

                int code = (int)response.StatusCode;
                string media = response.Content.Headers.ContentType?.MediaType ?? "?";

                if (!response.IsSuccessStatusCode)
                {
                    string errBody = await ReadLimitedAsync(response, 2000, ct).ConfigureAwait(false);
                    return $"ERRO: HTTP {code} ({media})\n{errBody}";
                }

                string body = await ReadLimitedAsync(response, MaxChars, ct).ConfigureAwait(false);
                var sb = new StringBuilder();
                sb.Append("HTTP ").Append(code).Append(" · ").Append(media);
                if (body.Length >= MaxChars)
                    sb.Append(" · truncado a ").Append(MaxChars).Append(" chars");
                sb.Append('\n').Append(body);
                return sb.ToString();
            }
            catch (TaskCanceledException)
            {
                return "ERRO: tempo limite (30s) ao buscar URL.";
            }
            catch (Exception ex)
            {
                return "ERRO: " + ex.Message;
            }
        }

        private static async Task<string> ReadLimitedAsync(
            HttpResponseMessage response, int maxChars, CancellationToken ct)
        {
            var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            using var reader = new System.IO.StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            var buf = new char[Math.Min(8192, maxChars)];
            var sb = new StringBuilder(Math.Min(maxChars, 4096));
            while (sb.Length < maxChars)
            {
                int toRead = Math.Min(buf.Length, maxChars - sb.Length);
                int n = await reader.ReadAsync(buf.AsMemory(0, toRead), ct).ConfigureAwait(false);
                if (n <= 0) break;
                sb.Append(buf, 0, n);
            }
            return sb.ToString();
        }
    }
}
