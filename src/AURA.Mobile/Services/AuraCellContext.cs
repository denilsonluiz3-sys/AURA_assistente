#if ANDROID
using System;
using System.Threading;
using AURA.Abstractions;

namespace AURA.Mobile.Services;

public sealed class AuraCellContext : IAuraCellContext
{
    public string CellId { get; }
    public CancellationToken CancellationToken { get; }
    public IAndroidCapabilityService Android { get; }

    public AuraCellContext(string cellId, IAndroidCapabilityService android, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(cellId)) throw new ArgumentException("CellId obrigatório.", nameof(cellId));
        CellId = cellId;
        Android = android ?? throw new ArgumentNullException(nameof(android));
        CancellationToken = ct;
    }
}
#endif
