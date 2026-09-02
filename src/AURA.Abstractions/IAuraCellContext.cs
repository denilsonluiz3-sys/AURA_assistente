using System.Collections.Generic;
using System.Threading;

namespace AURA.Abstractions;

public interface IAuraCellContext
{
    string CellId { get; }
    CancellationToken CancellationToken { get; }
    IReadOnlyDictionary<string, string> Arguments { get; }
    IDeviceDiagnosticCapability Device { get; }
    IBrowserCapability Browser { get; }
}
