using System.Threading;
using System.Threading.Tasks;
using AURA.Abstractions.Orchestration;

namespace AURA.Abstractions;

/// <summary>
/// Entrada única do Kernel autônomo da AURA.
/// A implementação executa capacidades locais sem depender de um LLM.
/// </summary>
public interface IKernel : IOrchestrator
{
    // Mantém a mesma assinatura do contrato de orquestração para evitar duas
    // entradas sobrecarregadas com ordens de parâmetros diferentes.
    Task<string> ExecuteAsync(
        string command,
        CancellationToken cancellationToken = default,
        bool confirmed = false);
}
