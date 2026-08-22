using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AURA.Agents
{
    public sealed record ToolResult(bool Success, string Output);

    public interface ITool
    {
        string Intent { get; }
        Task<ToolResult> ExecuteAsync(
            string command,
            Dictionary<string, string> parameters,
            CancellationToken ct = default);
    }

    /// <summary>Ferramenta adaptadora para integrar gradualmente o código existente.</summary>
    public sealed class DelegateTool : ITool
    {
        private readonly Func<string, Dictionary<string, string>, CancellationToken, Task<ToolResult>> _handler;
        public string Intent { get; }

        public DelegateTool(string intent, Func<string, Dictionary<string, string>, CancellationToken, Task<ToolResult>> handler)
        {
            Intent = intent ?? throw new ArgumentNullException(nameof(intent));
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        public Task<ToolResult> ExecuteAsync(string command, Dictionary<string, string> parameters, CancellationToken ct = default) =>
            _handler(command, parameters, ct);
    }

    public sealed class ToolResolver
    {
        private readonly Dictionary<string, ITool> _tools;

        public ToolResolver(IEnumerable<ITool> tools)
        {
            _tools = tools.ToDictionary(t => t.Intent, StringComparer.OrdinalIgnoreCase);
        }

        public ITool Resolve(string intent)
        {
            if (_tools.TryGetValue(intent, out ITool? tool)) return tool;
            return new DelegateTool("unknown", (_, _, _) => Task.FromResult(
                new ToolResult(false, "Não sei como executar essa solicitação localmente.")));
        }
    }
}
