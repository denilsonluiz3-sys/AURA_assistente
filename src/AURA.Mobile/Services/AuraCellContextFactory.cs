#if ANDROID
using System.Collections.Generic;
using System.Threading;
using AURA.Abstractions;
using AURA.Mobile.Pages;

namespace AURA.Mobile.Services;

public sealed class AuraCellContextFactory : IAuraCellContextFactory
{
    private readonly IAndroidCapabilityService _android;
    private readonly BrowserPage _browserPage;

    public AuraCellContextFactory(IAndroidCapabilityService android, BrowserPage browserPage)
    {
        _android = android ?? throw new System.ArgumentNullException(nameof(android));
        _browserPage = browserPage ?? throw new System.ArgumentNullException(nameof(browserPage));
    }

    public IAuraCellContext Create(string cellId, CancellationToken ct = default, IReadOnlyDictionary<string, string>? arguments = null)
        => new AuraCellContext(cellId, _android, _browserPage, ct, arguments);
}
#endif
