using System.Threading;
using System.Threading.Tasks;

namespace AURA.Core.Abstractions
{
    /// <summary>
    /// Represents an intelligent agent that can be started, stopped and
    /// queried. Implementations live in AURA.Agents (MemoryAgent,
    /// AutomationAgent, AIAgent wrapper).
    /// </summary>
    public interface IAgent
    {
        string Name { get; }

        string Description { get; }

        void Start();

        void Stop();

        /// <summary>
        /// Runs a one-shot query and returns the agent's answer.
        /// Implementations that don't support async can wrap synchronous
        /// logic in Task.FromResult.
        /// </summary>
        Task<string> AskAsync(string question, CancellationToken ct = default);
    }
}
