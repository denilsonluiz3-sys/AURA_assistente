using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace AURA.AI
{
    public sealed class WebFetchTool : AgentTool
    {
        private static readonly HttpClient _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        public override AgentToolDefinition Definition => new AgentToolDefinition
        {
            Name = "web_fetch",
            Description = "Busca conteúdo em sites de IA gratuitas via HTTP GET. 
                          Usa endpoints pré-definidos ou URLs fornecidos"
        };

        public override async Task<string> ExecuteAsync(string argumentsJson, CancellationToken ct = default)
        {
            string url;
            using (JsonDocument doc = JsonDocument.Parse(argumentsJson))
            {
                url = ReadString(doc.RootElement, "url") ?? string.Empty;
            }

            if (string.IsNullOrWhiteSpace(url))
            {
                return "ERRO: URL não fornecida.";
            }

            try
            {
                var response = await _httpClient.GetAsync(url, ct).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    return $"ERRO: HTTP {(int)response.StatusCode}";
                }

                string content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                return content;
            }
            catch (TaskCanceledException)
            {
                return "ERRO: tempo limite excedido ao buscar URL.";
            }
            catch (Exception ex)
            {
                return $"ERRO: {ex.Message}";
            }
        }
    }
}