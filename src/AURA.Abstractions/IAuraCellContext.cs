using System.Threading;

namespace AURA.Abstractions;

public interface IAuraCellContext
{
    string CellId { get; }
    CancellationToken CancellationToken { get; }
    IDeviceDiagnosticCapability Device { get; }
}
