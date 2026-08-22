#if ANDROID
using System;
using System.Threading;
using AURA.Abstractions;

namespace AURA.Mobile.Services;

public sealed class AuraCellContext : IAuraCellContext
{
    public string CellId { get; }
    public CancellationToken CancellationToken { get; }
    public IDeviceDiagnosticCapability Device { get; }

    public AuraCellContext(
        string cellId,
        IAndroidCapabilityService android,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(cellId)) throw new ArgumentException("CellId cannot be empty.", nameof(cellId));
        CellId = cellId;
        Device = new AndroidDeviceDiagnosticCapability(android ?? throw new ArgumentNullException(nameof(android)));
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
}
#endif
