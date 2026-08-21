#if ANDROID
using System;
using System.Threading;
using AURA.Abstractions;

namespace AURA.Mobile.Services;

public sealed class AuraCellContextFactory : IAuraCellContextFactory
{
    private readonly IAndroidCapabilityService _android;

    public AuraCellContextFactory(IAndroidCapabilityService android) =>
        _android = android ?? throw new ArgumentNullException(nameof(android));

    public IAuraCellContext Create(string cellId, CancellationToken ct = default) =>
        new AuraCellContext(cellId, _android, ct);
}
#endif
