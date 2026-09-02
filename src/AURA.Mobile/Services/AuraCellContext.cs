#if ANDROID
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using AURA.Abstractions;
using AURA.Mobile.Pages;

namespace AURA.Mobile.Services;

public sealed class AuraCellContext : IAuraCellContext
{
    public string CellId { get; }
    public CancellationToken CancellationToken { get; }
    public IReadOnlyDictionary<string, string> Arguments { get; }
    public IDeviceDiagnosticCapability Device { get; }
    public IBrowserCapability Browser { get; }

    public AuraCellContext(
        string cellId,
        IAndroidCapabilityService android,
        BrowserPage browserPage,
        CancellationToken ct = default,
        IReadOnlyDictionary<string, string>? arguments = null)
    {
        if (string.IsNullOrWhiteSpace(cellId)) throw new ArgumentException("CellId cannot be empty.", nameof(cellId));
        CellId = cellId;
        Device = new AndroidDeviceDiagnosticCapability(android ?? throw new ArgumentNullException(nameof(android)));
        Browser = new AndroidBrowserCapability(browserPage ?? throw new ArgumentNullException(nameof(browserPage)));
        Arguments = new ReadOnlyDictionary<string, string>(
            arguments is null ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) : new Dictionary<string, string>(arguments, StringComparer.OrdinalIgnoreCase));
        CancellationToken = ct;
    }

    private sealed class AndroidDeviceDiagnosticCapability : IDeviceDiagnosticCapability
    {
        private readonly IAndroidCapabilityService _android;
        public AndroidDeviceDiagnosticCapability(IAndroidCapabilityService android) => _android = android;
        public string GetDevice() => _android.GetDevice();
        public string GetProperties() => _android.GetProperties();
        public string GetBattery() => _android.GetBattery();
        public string GetNetwork() => _android.GetNetwork();
    }

    private sealed class AndroidBrowserCapability : IBrowserCapability
    {
        private readonly BrowserPage _page;
        public AndroidBrowserCapability(BrowserPage page) => _page = page;
        public bool IsAvailable => _page.AutomationAvailable || true;
        public Task<bool> OpenAsync(string url, CancellationToken ct = default) => _page.AutomationOpenAsync(url, ct);
        public Task<string> ReadAsync(string? selector = null, CancellationToken ct = default) => _page.AutomationReadAsync(selector, ct);
        public Task<bool> ClickAsync(string selector, CancellationToken ct = default) => _page.AutomationClickAsync(selector, ct);
        public Task<bool> TypeAsync(string selector, string text, CancellationToken ct = default) => _page.AutomationTypeAsync(selector, text, ct);
        public Task<bool> ScrollAsync(int pixels, CancellationToken ct = default) => _page.AutomationScrollAsync(pixels, ct);
        public Task<bool> BackAsync(CancellationToken ct = default) => _page.AutomationBackAsync(ct);
        public Task<bool> ForwardAsync(CancellationToken ct = default) => _page.AutomationForwardAsync(ct);
        public Task<bool> WaitAsync(int milliseconds, CancellationToken ct = default) => _page.AutomationWaitAsync(milliseconds, ct);
        public Task<string?> ScreenshotAsync(CancellationToken ct = default) => _page.AutomationScreenshotAsync(ct);
    }
}
#endif
