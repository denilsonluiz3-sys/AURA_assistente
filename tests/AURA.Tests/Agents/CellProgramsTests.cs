using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AURA.Abstractions;
using AURA.Agents;
using AURA.Agents.Programs;
using Xunit;

namespace AURA.Tests.Agents;

public sealed class CellProgramsTests
{
    [Fact]
    public void Resolver_RecognizesDeviceDiagnostic()
    {
        var resolver = new HeuristicIntentResolver();

        var result = resolver.Resolve("diagnóstico do aparelho");

        Assert.Equal("android", result.Intent);
        Assert.Equal("device-diagnostic", result.Parameters["action"]);
        Assert.True(result.Confidence >= 0.95);
    }

    [Fact]
    public void Policy_AllowsKnownReadCapabilities()
    {
        var policy = new PolicyGuard();

        var result = policy.Authorize(
            new[] { "android.device.read", "android.battery.read", "android.network.read" },
            "diagnóstico do aparelho");

        Assert.Equal(AuthorizationDecision.Allowed, result.Decision);
    }

    [Fact]
    public void Policy_BlocksUnknownCapability()
    {
        var policy = new PolicyGuard();

        var result = policy.Authorize(
            new[] { "android.shell.execute" },
            "diagnóstico do aparelho");

        Assert.Equal(AuthorizationDecision.Blocked, result.Decision);
    }

    [Fact]
    public void Registry_ResolvesCanonicalNameCaseInsensitively()
    {
        var registry = new CellProgramRegistry();
        var program = new StubProgram("device-diagnostic");
        registry.Register(program);

        Assert.Same(program, registry.Resolve("DEVICE-DIAGNOSTIC"));
    }

    private sealed class StubProgram : IAuraCellProgram
    {
        public StubProgram(string name) => Name = name;
        public string Name { get; }
        public IReadOnlyCollection<string> RequiredCapabilities { get; } = new[] { "android.device.read" };

        public Task<CellProgramResult> ExecuteAsync(IAuraCellContext context, CancellationToken ct = default)
            => Task.FromResult(CellProgramResult.Ok("ok"));
    }
}
