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

    [Fact]
    public async Task BrowserReadIncludesStructuredDom()
    {
        var browser = new FakeBrowserCapability();
        var context = new FakeContext(browser, new Dictionary<string, string>
        {
            ["selector"] = "body"
        });

        var result = await new BrowserReadCellProgram().ExecuteAsync(context);

        Assert.True(result.IsSuccess);
        Assert.Contains("Dom", result.OutputJson);
        Assert.Contains("links", result.OutputJson);
        Assert.Contains("buttons", result.OutputJson);
        Assert.Contains("inputs", result.OutputJson);
        Assert.Contains("dom-1", result.OutputJson);
    }

    [Fact]
    public async Task SupportsReadClickTypeScrollBackForwardWaitAndScreenshot()
    {
        var browser = new FakeBrowserCapability();
        var context = new FakeContext(browser, new Dictionary<string, string>
        {
            ["selector"] = "#login",
            ["text"] = "AURA",
            ["pixels"] = "500",
            ["milliseconds"] = "1"
        });

        Assert.True((await new BrowserReadCellProgram().ExecuteAsync(context)).IsSuccess);
        Assert.True((await new BrowserClickCellProgram().ExecuteAsync(context)).IsSuccess);
        Assert.True((await new BrowserTypeCellProgram().ExecuteAsync(context)).IsSuccess);
        Assert.True((await new BrowserScrollCellProgram().ExecuteAsync(context)).IsSuccess);
        Assert.True((await new BrowserBackCellProgram().ExecuteAsync(context)).IsSuccess);
        Assert.True((await new BrowserForwardCellProgram().ExecuteAsync(context)).IsSuccess);
        Assert.True((await new BrowserWaitCellProgram().ExecuteAsync(context)).IsSuccess);
        Assert.True((await new BrowserScreenshotCellProgram().ExecuteAsync(context)).IsSuccess);
        Assert.Equal("#login", browser.LastSelector);
        Assert.Equal("AURA", browser.LastText);
    }

    private sealed class FakeBrowserCapability : IBrowserCapability
    {
        public bool IsAvailable => true;
        public string? LastUrl { get; private set; }
        public string? LastSelector { get; private set; }
        public string? LastText { get; private set; }

        public Task<bool> OpenAsync(string url, CancellationToken ct = default)
        {
            LastUrl = url;
            return Task.FromResult(true);
        }

        public Task<string> ReadAsync(string? selector = null, CancellationToken ct = default)
        {
            LastSelector = selector;
            return Task.FromResult("AURA page");
        }

        public Task<string> ReadDomAsync(string? selector = null, CancellationToken ct = default)
        {
            LastSelector = selector;
            return Task.FromResult("{\"ok\":true,\"url\":\"https://example.com\",\"title\":\"AURA\",\"nodeCount\":1,\"truncated\":false,\"dom\":{\"id\":\"dom-1\",\"tag\":\"body\",\"text\":\"AURA page\",\"attributes\":{}},\"links\":[],\"buttons\":[],\"inputs\":[]}");
        }

        public Task<bool> ClickAsync(string selector, CancellationToken ct = default)
        {
            LastSelector = selector;
            return Task.FromResult(true);
        }

        public Task<bool> TypeAsync(string selector, string text, CancellationToken ct = default)
        {
            LastSelector = selector;
            LastText = text;
            return Task.FromResult(true);
        }

        public Task<bool> ScrollAsync(int pixels, CancellationToken ct = default) => Task.FromResult(true);
        public Task<bool> BackAsync(CancellationToken ct = default) => Task.FromResult(true);
        public Task<bool> ForwardAsync(CancellationToken ct = default) => Task.FromResult(true);
        public Task<bool> WaitAsync(int milliseconds, CancellationToken ct = default) => Task.FromResult(true);
        public Task<string?> ScreenshotAsync(CancellationToken ct = default) => Task.FromResult<string?>("/cache/aura-browser.png");
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
