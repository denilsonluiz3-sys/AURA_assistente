using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AURA.Memory;

namespace AURA.AI
{
    /// <summary>
    /// Busca na memória procedural (SolutionStore) por tarefas similares.
    /// </summary>
    public sealed class SearchMemoryTool : AgentTool
    {
        private readonly SolutionStore _memory;

        public SearchMemoryTool(SolutionStore memory)
        {
            _memory = memory ?? throw new ArgumentNullException(nameof(memory));
        }

        public override AgentToolDefinition Definition => new AgentToolDefinition
        {
            Name = "search_memory",
            Description = "Busca na memória procedural por tarefas similares já executadas com sucesso. " +
                         "Retorna a ação tomada e o resultado.",
            Parameters =
            {
                ["query"] = new AgentToolParameter
                {
                    Type = "string",
                    Description = "Descrição da tarefa ou comando a buscar."
                }
            },
            Required = { "query" }
        };

        public override Task<string> ExecuteAsync(string argumentsJson, CancellationToken ct = default)
        {
            string query;
            using (JsonDocument doc = JsonDocument.Parse(argumentsJson))
            {
                query = ReadString(doc.RootElement, "query") ?? string.Empty;
            }

            if (string.IsNullOrWhiteSpace(query))
            {
                return Task.FromResult("ERRO: query vazia.");
            }

            var hit = _memory.FindBestMatch(query);
            if (hit == null)
            {
                return Task.FromResult("Nenhuma memória encontrada para esta tarefa.");
            }

            var result = new
            {
                found = true,
                id = hit.Id,
                task = hit.TaskDescription,
                action = hit.ActionTaken,
                result = hit.ResultDetails,
                timestamp = hit.Timestamp,
                days_ago = (DateTime.UtcNow - hit.Timestamp).TotalDays
            };

            string json = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
            return Task.FromResult(json);
        }
    }
}