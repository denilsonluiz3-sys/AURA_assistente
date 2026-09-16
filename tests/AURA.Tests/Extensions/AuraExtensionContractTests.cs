using AURA.Core.Extensions;

namespace AURA.Tests.Extensions;

public sealed class AuraExtensionContractTests
{
    [Fact]
    public void ValidManifestUsesExactOriginAndMinimalPermissions()
    {
        var manifest = ValidManifest();
        AuraExtensionValidationResult result = AuraExtensionManifestValidator.Validate(manifest);
        Assert.True(result.Valid, string.Join("; ", result.Errors));
        Assert.True(AuraExtensionOriginMatcher.IsAllowed("https://example.com/app/page", manifest));
        Assert.False(AuraExtensionOriginMatcher.IsAllowed("https://sub.example.com/app/page", manifest));
        Assert.False(AuraExtensionOriginMatcher.IsAllowed("http://example.com/app/page", manifest));
    }

    [Fact]
    public void ManifestStartsDisabledAndRejectsUnsafeScriptAndPermission()
    {
        var manifest = ValidManifest();
        manifest.EnabledByDefault = true;
        manifest.Permissions.Add("native.messaging");
        manifest.ContentScripts[0].Path = "../escape.js";
        AuraExtensionValidationResult result = AuraExtensionManifestValidator.Validate(manifest);
        Assert.False(result.Valid);
        Assert.Contains(result.Errors, e => e.Contains("desativadas"));
        Assert.Contains(result.Errors, e => e.Contains("não suportada"));
        Assert.Contains(result.Errors, e => e.Contains("caminho"));
    }

    [Fact]
    public async Task StorageIsPrivatePerExtensionAndRejectsTraversal()
    {
        string root = Path.Combine(Path.GetTempPath(), "aura-ext-" + Guid.NewGuid().ToString("N"));
        try
        {
            var storage = new AuraExtensionStorage(root);
            await storage.WriteAsync("com.aura.test", "key", "value");
            Assert.Equal("value", await storage.ReadAsync("com.aura.test", "key"));
            Assert.Null(await storage.ReadAsync("com.aura.other", "key"));
            await Assert.ThrowsAsync<ArgumentException>(() => storage.WriteAsync("com.aura.test", "../escape", "x"));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public async Task StateIsDisabledUntilExplicitlyEnabled()
    {
        string root = Path.Combine(Path.GetTempPath(), "aura-ext-state-" + Guid.NewGuid().ToString("N"));
        try
        {
            var states = new AuraExtensionStateStore(root);
            Assert.False(states.IsEnabled("com.aura.test"));
            await states.SetEnabledAsync("com.aura.test", true);
            Assert.True(states.IsEnabled("com.aura.test"));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    private static AuraExtensionManifest ValidManifest() => new()
    {
        Id = "com.aura.example",
        Name = "Example",
        Version = "1.0.0",
        EnabledByDefault = false,
        Matches = { "https://example.com/app/*" },
        ContentScripts = { new AuraContentScript { Id = "main", Path = "content/main.js", RunAt = "document-end" } },
        Permissions = { AuraExtensionPermissions.PageDom, AuraExtensionPermissions.ExtensionStorage }
    };
}
