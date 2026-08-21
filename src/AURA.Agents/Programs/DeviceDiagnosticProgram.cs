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

        var data = new
        {
            Device = context.Android.GetDevice(),
            AndroidProperties = context.Android.GetProperties(),
            Battery = context.Android.GetBattery(),
            Network = context.Android.GetNetwork()
        };

        return Task.FromResult(CellProgramResult.Ok(data));
    }
}
