using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AURA.Memory;

namespace AURA.AI
{
    public sealed class SaveMemoryTool : AgentTool
    {
        private readonly SolutionStore _solutions;

        public SaveMemoryTool(SolutionStore solutions)
        {
            _solutions = solutions ?? throw new ArgumentNullException(nameof(solutions));
        }

        public override AgentToolDefinition Definition => new AgentToolDefinition
        {
            Name = "memory_save",
            Description = "Salva uma tarefa e seu resultado na memória procedural para referência futura.",
            Parameters =
            {
                ["task"] = new AgentToolParameter { Type = "string", Description = "Descrição da tarefa." },
                ["action"] = new AgentToolParameter { Type = "string", Description = "Ação tomada." },
                ["result"] = new AgentToolParameter { Type = "string", Description = "Resultado obtido." },
                ["success"] = new AgentToolParameter { Type = "string", Description = "true/false (padrão: true)." }
            },
            Required = { "task", "action", "result" }
        };

        public override Task<string> ExecuteAsync(string argumentsJson, CancellationToken ct = default)
        {
            string task, action, result;
            bool success = true;
            using (JsonDocument doc = JsonDocument.Parse(argumentsJson))
            {
                JsonElement root = doc.RootElement;
                task = ReadString(root, "task") ?? string.Empty;
                action = ReadString(root, "action") ?? string.Empty;
                result = ReadString(root, "result") ?? string.Empty;
                string? s = ReadString(root, "success");
                if (s != null) success = s.Equals("true", StringComparison.OrdinalIgnoreCase);
            }

            if (string.IsNullOrWhiteSpace(task))
                return Task.FromResult("ERRO: task vazio.");

            _solutions.Record(task, action, result, success);
            return Task.FromResult("OK: memória salva.");
        }
    }

    public sealed class ConversationMemoryTool : AgentTool
    {
        private readonly MemoryStore _memory;

        public ConversationMemoryTool(MemoryStore memory)
        {
            _memory = memory ?? throw new ArgumentNullException(nameof(memory));
        }

        public override AgentToolDefinition Definition => new AgentToolDefinition
        {
            Name = "memory_conversation",
            Description = "Lê as últimas entradas da memória de conversas.",
            Parameters =
            {
                ["count"] = new AgentToolParameter { Type = "string", Description = "Número de entradas (padrão: 12)." }
            }
        };

        public override Task<string> ExecuteAsync(string argumentsJson, CancellationToken ct = default)
        {
            int count = 12;
            try
            {
                using JsonDocument doc = JsonDocument.Parse(argumentsJson);
                string? c = ReadString(doc.RootElement, "count");
                if (c != null) int.TryParse(c, out count);
            }
            catch { /* ignore */ }

            var entries = _memory.Read(tail: Math.Clamp(count, 1, 64));
            if (entries.Count == 0)
                return Task.FromResult("Nenhuma entrada na memória.");

            var sb = new StringBuilder();
            sb.AppendLine($"Últimas {entries.Count} entradas:");
            foreach (var e in entries)
            {
                string text = (e.Text ?? "").Replace('\n', ' ');
                if (text.Length > 100) text = text[..100] + "…";
                sb.AppendLine($"[{e.Role ?? e.Kind.ToString()}] {text}");
            }
            return Task.FromResult(sb.ToString().TrimEnd());
        }
    }
}
