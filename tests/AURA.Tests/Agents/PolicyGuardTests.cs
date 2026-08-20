using AURA.Agents;

namespace AURA.Tests.Agents;

public sealed class PolicyGuardTests
{
    private readonly PolicyGuard _guard = new();

    [Fact]
    public void Execute_ShouldRequireConfirmation()
    {
        AuthorizationResult result = _guard.Authorize("execute", "execute /tmp/test.sh");
        Assert.Equal(AuthorizationDecision.RequiresConfirmation, result.Decision);
        Assert.Contains("confirmação", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BlockedIntent_ShouldBeBlocked()
    {
        AuthorizationResult result = _guard.Authorize("blocked", "ação proibida");
        Assert.Equal(AuthorizationDecision.Blocked, result.Decision);
    }

    [Fact]
    public void Search_ShouldBeAllowed()
    {
        AuthorizationResult result = _guard.Authorize("search", "pesquise aura");
        Assert.Equal(AuthorizationDecision.Allowed, result.Decision);
    }
}
