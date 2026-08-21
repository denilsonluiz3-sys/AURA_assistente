using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AURA.Abstractions;

namespace AURA.Agents.Programs;

public sealed class DeviceDiagnosticProgram : IAuraCellProgram
{
    public string Name => "device-diagnostic";

    public IReadOnlyCollection<string> RequiredCapabilities { get; } = new[]
    {
        "android.device.read",
        "android.battery.read",
        "android.network.read"
    };

    public Task<CellProgramResult> ExecuteAsync(IAuraCellContext context, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var device = context.Device.GetDevice();
        var properties = context.Device.GetProperties();
        var battery = context.Device.GetBattery();
        var network = context.Device.GetNetwork();

        return Task.FromResult(CellProgramResult.Ok(new
        {
            Device = device,
            DeviceProperties = properties,
            Battery = battery,
            Network = network
        }));
    }
}
