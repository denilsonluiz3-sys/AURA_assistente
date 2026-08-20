using System.Threading;
using System.Threading.Tasks;

namespace AURA.Abstractions.Orchestration;

public interface IOrchestrator
{
    Task<string> ExecuteAsync(
        string userCommand,
        CancellationToken cancellationToken = default,
        bool confirmed = false);
}
