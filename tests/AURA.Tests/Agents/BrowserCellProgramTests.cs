using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AURA.Abstractions;
using AURA.Agents.Programs;
using Xunit;

namespace AURA.Tests.Agents;

public sealed class BrowserCellProgramTests
{
    [Fact]
    public async Task OpensHttpsUrlThroughBrowserCapability()
    {
        var browser = new FakeBrowserCapability();
        var context = new FakeContext(browser, new Dictionary<string, string> { ["url"] = "https://example.com" });

        var result = await new BrowserCellProgram().ExecuteAsync(context);

        Assert.True(result.IsSuccess);
        Assert.Equal("https://example.com", browser.LastUrl);
    }

    [Fact]
    public async Task RejectsNonHttpUrl()
    {
        var browser = new FakeBrowserCapability();
        var context = new FakeContext(browser, new Dictionary<string, string> { ["url"] = "file:///etc/passwd" });

        var result = await new BrowserCellProgram().ExecuteAsync(context);

        Assert.False(result.IsSuccess);
        Assert.Null(browser.LastUrl);
    }

    [Fact]
    public async Task RequiresUrlArgument()
    {
        var result = await new BrowserCellProgram().ExecuteAsync(
            new FakeContext(new FakeBrowserCapability(), new Dictionary<string, string>()));

        Assert.False(result.IsSuccess);
    }

    private sealed class FakeBrowserCapability : IBrowserCapability
    {
        public bool IsAvailable => true;
        public string? LastUrl { get; private set; }

        public Task<bool> OpenAsync(string url, CancellationToken ct = default)
        {
            LastUrl = url;
            return Task.FromResult(true);
        }
    }

    private sealed class FakeContext : IAuraCellContext
    {
        public FakeContext(IBrowserCapability browser, IReadOnlyDictionary<string, string> arguments)
        {
            Browser = browser;
            Arguments = arguments;
        }

        public string CellId => "test-browser";
        public CancellationToken CancellationToken => CancellationToken.None;
        public IReadOnlyDictionary<string, string> Arguments { get; }
        public IDeviceDiagnosticCapability Device => throw new System.NotImplementedException();
        public IBrowserCapability Browser { get; }
    }
}
