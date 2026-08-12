using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AURA.Core.Abstractions;
using AURA.Core.Logging;
using AURA.Memory;

namespace AURA.Agents
{
    /// <summary>
    /// Agent que responde perguntas consultando a memória de curto prazo
    /// do usuário (~/AURA/memory.json). Útil para "o que eu perguntei antes?",
    /// "resuma meu histórico", etc. — sem chamar rede externa.
    /// </summary>
    public sealed class MemoryAgent : IAgent
    {
        private readonly MemoryStore _store;
        private readonly ILogger _logger;

        public string Name => "memory";
        public string Description => "Consulta e resume a memória de conversas anteriores (sem rede)";

        public MemoryAgent(MemoryStore store, ILogger logger = null)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _logger = logger ?? new ConsoleLogger();
        }

        public void Start() => _logger.Info("[MemoryAgent] iniciado");

        public void Stop() => _logger.Info("[MemoryAgent] parado");

        public Task<string> AskAsync(string question, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(question))
            {
                return Task.FromResult("Pergunta vazia.");
            }

            var entries = _store.Read(tail: 32);
            if (entries.Count == 0)
            {
                return Task.FromResult("Nenhuma entrada na memória ainda.");
            }

            var sb = new StringBuilder();
            sb.AppendLine("Últimas entradas na memória:");
            foreach (var e in entries)
            {
                if (e.Kind == MemoryKind.Turn)
                {
                    sb.AppendLine($"  [{e.TimestampUtc:HH:mm}] {e.Role}: {e.Text}");
                }
                else
                {
                    sb.AppendLine($"  [{e.TimestampUtc:HH:mm}] célula '{e.CellId}': {e.Detail}");
                }
            }

            return Task.FromResult(sb.ToString().TrimEnd());
        }
    }
}
