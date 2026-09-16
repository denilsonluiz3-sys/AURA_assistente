using AURA.Core.Security;

namespace AURA.Tests;

public sealed class WebSecurityPolicyTests
{
    [Fact]
    public void AllowsHttpsWithoutCredentialsOrNonStandardPort()
    {
        Assert.True(WebSecurityPolicy.TryValidateHttpUrl("https://example.com/path", out Uri uri));
        Assert.Equal("https", uri.Scheme);
        Assert.False(WebSecurityPolicy.TryValidateHttpUrl("https://user:pass@example.com", out _));
        Assert.False(WebSecurityPolicy.TryValidateHttpUrl("https://example.com:8443", out _));
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("file:///etc/passwd")]
    [InlineData("http://localhost")]
    [InlineData("https://127.0.0.1")]
    [InlineData("https://192.168.1.10")]
    public void RejectsUnsafeSchemesAndLocalHostsWhenRequested(string value)
    {
        Assert.False(WebSecurityPolicy.TryValidateHttpUrl(value, out _, rejectLocalHost: true));
    }

    [Fact]
    public void RedactsQueryAndFragmentFromLogs()
    {
        string result = WebSecurityPolicy.RedactForLog("https://example.com/path?token=secret#fragment");
        Assert.Equal("https://example.com/path", result);
        Assert.DoesNotContain("secret", result);
        Assert.DoesNotContain("fragment", result);
    }
}
