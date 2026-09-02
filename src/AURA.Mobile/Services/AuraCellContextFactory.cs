#if ANDROID
using System.Collections.Generic;
using System.Threading;
using AURA.Abstractions;

namespace AURA.Mobile.Services;

public sealed class AuraCellContextFactory : IAuraCellContextFactory
{
    private readonly IAndroidCapabilityService _android;

    public AuraCellContextFactory(IAndroidCapabilityService android)
    {
        _android = android ?? throw new System.ArgumentNullException(nameof(android));
    }

    public IAuraCellContext Create(string cellId, CancellationToken ct = default, IReadOnlyDictionary<string, string>? arguments = null)
        => new AuraCellContext(cellId, _android, ct, arguments);
}
#endif
