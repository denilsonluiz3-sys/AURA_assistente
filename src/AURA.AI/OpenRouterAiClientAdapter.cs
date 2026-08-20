using System;
using System.Threading;
using System.Threading.Tasks;
using AURA.Abstractions;

namespace AURA.AI
{
    /// <summary>
    /// Adapter que mantém OpenRouterClient desacoplado do fluxo principal da AURA.
    /// </summary>
    public sealed class OpenRouterAiClientAdapter : IAiClient
    {
        private readonly OpenRouterClient _client;

        public OpenRouterAiClientAdapter(OpenRouterClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public Task<string> ChatAsync(string question, CancellationToken ct = default) =>
            _client.ChatAsync(question, ct: ct);
    }
}
