using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AURA.AI.UniversalAI;
using AURA.Core.Logging;
using AURA.Memory;

namespace AURA.AI
{
    /// <summary>Serviço de chat apoiado exclusivamente pelo cliente universal.</summary>
    public static class AiAssistantService
    {
        public static async Task<string> AskAsync(
            string question,
            MemoryStore? memory = null,
            ILogger? logger = null,
            IUniversalAiClient? client = null,
            HttpClient? http = null,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(question))
                throw new ArgumentException("A pergunta não pode ser vazia.", nameof(question));
            if (client == null)
                throw new InvalidOperationException("Cliente universal não configurado. A aplicação deve fornecer a configuração escolhida pelo usuário.");

            ILogger log = logger ?? new ConsoleLogger();
            if (memory != null) memory.Append(MemoryEntry.Question(question));

            string answer = await client.ChatAsync(question, http, null, ct).ConfigureAwait(false);

            if (memory != null)
            {
                memory.Append(MemoryEntry.Answer(answer));
                log.Info("AI: pergunta e resposta armazenadas.");
            }
            return answer;
        }
    }
}