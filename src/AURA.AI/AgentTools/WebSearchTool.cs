using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AURA.Core.Abstractions;

namespace AURA.AI
{
    /// <summary>
    /// Ferramenta de busca web via <see cref="IWebSearch"/> (Bing/DuckDuckGo,
    /// sem API key).
    /// </summary>
    public sealed class WebSearchTool : AgentTool
    {
        private readonly IWebSearch _webSearch;

        public WebSearchTool(IWebSearch webSearch)
        {
            _webSearch = webSearch ?? throw new ArgumentNullException(nameof(webSearch));
        }

        public override AgentToolDefinition Definition => new AgentToolDefinition
        {
            Name = "web_search",
            Description = "Busca informações na web SEM API key usando Bing/DuckDuckGo.",
            Parameters =
            {
                ["query"] = new AgentToolParameter
                {
                    Type = "string",
                    Description = "Termo ou pergunta para pesquisar"
                }
            },
            Required = { "query" }
        };

        public override async Task<string> ExecuteAsync(string argumentsJson, CancellationToken ct = default)
        {
            string query;
            using (JsonDocument doc = JsonDocument.Parse(argumentsJson))
            {
                query = doc.RootElement.TryGetProperty("query", out var q) ? q.GetString() ?? "" : "";
            }

            if (string.IsNullOrWhiteSpace(query))
                return "ERRO: query vazia.";

            try
            {
                string result = await _webSearch.SearchWithRefinementAsync(query, ct);
                return string.IsNullOrWhiteSpace(result)
                    ? "Nenhum resultado encontrado para: " + query
                    : result;
            }
            catch (Exception ex)
            {
                return "ERRO na busca web: " + ex.Message;
            }
        }
    }
}