using System.Threading;
using System.Threading.Tasks;

namespace AURA.Abstractions.Process
{
    /// <summary>
    /// Orquestra uma solicitação como um processo jurídico: percorre as fases
    /// (pré-processual → conhecimento → decisão → recursal → execução →
    /// arquivamento), decidindo em cada uma com memória, busca, execução e IA.
    /// É a porta única que une chat e agentes.
    /// </summary>
    public interface IProcessOrchestrator
    {
        Task<string> RunAsync(
            string userCommand,
            LlmHandler? llm = null,
            CancellationToken cancellationToken = default);

        Task<string> HandleUserInputAsync(
            string userCommand,
            LlmHandler? llm = null,
            CancellationToken cancellationToken = default);

        ProcessState GetCurrentState(string? processId = null);
    }

    /// <summary>Delegado opcional de raciocínio (LLM) usado na fase decisória.</summary>
    public delegate Task<string> LlmHandler(string prompt, CancellationToken cancellationToken);
}