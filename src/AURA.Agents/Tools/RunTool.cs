using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AURA.Agents
{
    /// <summary>Ferramenta concreta para execução via Runner/SimulationRuntime existentes.</summary>
    public sealed class RunTool : ITool
    {
        private readonly Func<string, CancellationToken, Task<ToolResult>> _run;

        public RunTool(Func<string, CancellationToken, Task<ToolResult>> run)
        {
            _run = run ?? throw new ArgumentNullException(nameof(run));
        }

        public string Intent => "execute";

        public Task<ToolResult> ExecuteAsync(
            string command,
            Dictionary<string, string> parameters,
            CancellationToken ct = default) =>
            _run(command, ct);
    }
}
