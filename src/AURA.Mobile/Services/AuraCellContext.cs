#if ANDROID
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using AURA.Abstractions;

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
        CancellationToken ct = default,
        IReadOnlyDictionary<string, string>? arguments = null)
    {
        if (string.IsNullOrWhiteSpace(cellId)) throw new ArgumentException("CellId cannot be empty.", nameof(cellId));
        CellId = cellId;
        Device = new AndroidDeviceDiagnosticCapability(android ?? throw new ArgumentNullException(nameof(android)));
        Browser = new AndroidBrowserCapability();
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
        public bool IsAvailable => true;

        public async Task<bool> OpenAsync(string url, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;
            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) return false;
            try
            {
                return await Microsoft.Maui.ApplicationModel.Browser.Default.OpenAsync(
                    uri, Microsoft.Maui.ApplicationModel.BrowserLaunchMode.External).ConfigureAwait(false);
            }
            catch
            {
                return false;
            }
        }
    }
}
#endif
