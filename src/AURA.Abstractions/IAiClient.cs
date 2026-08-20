using System.Threading;
using System.Threading.Tasks;

namespace AURA.Abstractions
{
    /// <summary>
    /// Abstração mínima para um cliente de IA usado apenas como fallback.
    /// O fluxo principal da AURA não depende desta interface.
    /// </summary>
    public interface IAiClient
    {
        Task<string> ChatAsync(string question, CancellationToken ct = default);
    }
}
