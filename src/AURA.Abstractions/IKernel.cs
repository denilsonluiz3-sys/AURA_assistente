using System.Threading;
using System.Threading.Tasks;
using AURA.Abstractions.Orchestration;

namespace AURA.Abstractions;

/// <summary>
/// Entrada única do Kernel autônomo da AURA.
/// A implementação deve conseguir executar capacidades locais sem depender de um LLM.
/// </summary>
public interface IKernel : IOrchestrator
{
    Task<string> ExecuteAsync(
        string command,
        bool confirmed = false,
        CancellationToken cancellationToken = default);
}
