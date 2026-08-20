using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AURA.Agents
{
    /// <summary>Ferramenta concreta de pesquisa, mantendo a implementação de rede fora do contrato ITool.</summary>
    public sealed class SearchTool : ITool
    {
        private readonly Func<string, CancellationToken, Task<string>> _search;

        public SearchTool(Func<string, CancellationToken, Task<string>> search)
        {
            _search = search ?? throw new ArgumentNullException(nameof(search));
        }

        public string Intent => "search";

        public async Task<ToolResult> ExecuteAsync(
            string command,
            Dictionary<string, string> parameters,
            CancellationToken ct = default)
        {
            string query = parameters.TryGetValue("query", out string? value) && !string.IsNullOrWhiteSpace(value)
                ? value
                : command;

            if (string.IsNullOrWhiteSpace(query))
                return new ToolResult(false, "Consulta de pesquisa vazia.");

            string output = await _search(query, ct).ConfigureAwait(false);
            return new ToolResult(
                !string.IsNullOrWhiteSpace(output) &&
                !output.StartsWith("Falha na busca:", StringComparison.OrdinalIgnoreCase),
                output);
        }
    }
}
