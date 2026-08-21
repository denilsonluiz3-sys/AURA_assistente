using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AURA.Abstractions;

public interface IAuraCellProgram
{
    string Name { get; }
    IReadOnlyCollection<string> RequiredCapabilities { get; }
    Task<CellProgramResult> ExecuteAsync(IAuraCellContext context, CancellationToken ct = default);
}
