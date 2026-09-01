using System.Net.Http;
using AURA.AI.UniversalAI;
using AURA.Core.Logging;
using AURA.Memory;

namespace AURA.AI;

/// <summary>Serviço de chat apoiado exclusivamente pelo cliente universal.</summary>
public sealed class AiAssistant
{
    private readonly IUniversalAiClient _client;
    private readonly MemoryStore _memory;
    private readonly ILogger _logger;

    public AiAssistant(IUniversalAiClient client, MemoryStore memory, ILogger? logger = null)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _memory = memory ?? throw new ArgumentNullException(nameof(memory));
        _logger = logger ?? new ConsoleLogger();
    }

    public async Task<string> AskAsync(string question, HttpClient? httpClient = null, CancellationToken ct = default)
    {
        _memory.Append(MemoryEntry.Question(question));
        var answer = await _client.ChatAsync(question, httpClient, ct).ConfigureAwait(false);
        _memory.Append(MemoryEntry.Answer(answer));
        _logger.Info("AI: pergunta registrada e respondida.");
        return answer;
    }
}
