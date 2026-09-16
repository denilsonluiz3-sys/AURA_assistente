using System.Text.Json;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using AURA.Core.Extensions;
using AURA.Core.Security;

namespace AURA.Mobile.Extensions;

public sealed record BrowserExtensionStatus(string Id, string Name, string Version, bool Enabled, bool Valid, string? Error);
public sealed record BundledAuraExtension(string Key, string Id, string Name);

/// <summary>Executa content scripts próprios somente no BrowserPage. Não é usado pelo Web AI.</summary>
public sealed class BrowserExtensionCoordinator
{
    private const int MaxScriptBytes = 256 * 1024;
    private const string DemoPackage = "Extensions/demo/";
    private static readonly IReadOnlyList<BundledAuraExtension> BundledCatalog = new[]
    {
        new BundledAuraExtension("demo", "com.aura.demo.extension", "Demonstração AURA"),
        new BundledAuraExtension("security", "com.aura.security.guard", "AURA Segurança da Página"),
        new BundledAuraExtension("integration", "com.aura.integration.context", "AURA Contexto da Página"),
        new BundledAuraExtension("tools", "com.aura.tools.reading", "AURA Ferramentas de Leitura")
    };
    private readonly string _installedRoot = Path.Combine(FileSystem.AppDataDirectory, "extensions", "installed");
    private readonly AuraExtensionStateStore _states;
    private readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };
    private readonly HashSet<string> _injected = new(StringComparer.Ordinal);
    private readonly object _gate = new();

    public BrowserExtensionCoordinator()
    {
        _states = new AuraExtensionStateStore(Path.Combine(FileSystem.AppDataDirectory, "extensions", "state"));
    }

    public async Task<IReadOnlyList<BrowserExtensionStatus>> GetInstalledAsync(CancellationToken ct = default)
    {
        var result = new List<BrowserExtensionStatus>();
        if (!Directory.Exists(_installedRoot)) return result;
        foreach (string packageRoot in Directory.EnumerateDirectories(_installedRoot))
        {
            ct.ThrowIfCancellationRequested();
            string manifestPath = Path.Combine(packageRoot, "manifest.json");
            if (!File.Exists(manifestPath)) continue;
            try
            {
                AuraExtensionManifest? manifest = JsonSerializer.Deserialize<AuraExtensionManifest>(await File.ReadAllTextAsync(manifestPath, ct), _json);
                AuraExtensionValidationResult validation = AuraExtensionManifestValidator.Validate(manifest);
                if (manifest is null)
                {
                    result.Add(new("", Path.GetFileName(packageRoot), "", false, false, "manifesto ausente"));
                    continue;
                }
                result.Add(new(manifest.Id, manifest.Name, manifest.Version, _states.IsEnabled(manifest.Id), validation.Valid, validation.Valid ? null : string.Join(", ", validation.Errors)));
            }
            catch (Exception ex)
            {
                result.Add(new("", Path.GetFileName(packageRoot), "", false, false, ex.Message));
            }
        }
        return result;
    }

    public IReadOnlyList<BundledAuraExtension> GetBundledCatalog() => BundledCatalog;

    public Task<BrowserExtensionStatus> InstallBundledDemoAsync(CancellationToken ct = default) => InstallBundledAsync("demo", "Demonstração AURA", ct);

    public async Task<BrowserExtensionStatus> InstallBundledAsync(string key, string? fallbackName = null, CancellationToken ct = default)
    {
        string package = key == "demo" ? "demo" : key;
        string staging = Path.Combine(FileSystem.CacheDirectory, "aura-extension-" + key + "-" + Guid.NewGuid().ToString("N"));
        string destination;
        try
        {
            Directory.CreateDirectory(staging);
            Directory.CreateDirectory(Path.Combine(staging, "content"));
            await CopyBundledAsync("Extensions/" + package + "/manifest.json", Path.Combine(staging, "manifest.json"), ct);
            await CopyBundledAsync("Extensions/" + package + "/content/main.js", Path.Combine(staging, "content", "main.js"), ct);
            AuraExtensionManifest? manifest = JsonSerializer.Deserialize<AuraExtensionManifest>(await File.ReadAllTextAsync(Path.Combine(staging, "manifest.json"), ct), _json);
            AuraExtensionValidationResult validation = AuraExtensionManifestValidator.Validate(manifest);
            if (manifest is null || !validation.Valid) throw new InvalidOperationException(string.Join(", ", validation.Errors));
            destination = Path.Combine(_installedRoot, manifest.Id);
            Directory.CreateDirectory(_installedRoot);
            if (Directory.Exists(destination)) Directory.Delete(destination, true);
            Directory.Move(staging, destination);
            await _states.SetEnabledAsync(manifest.Id, false, ct);
            return new(manifest.Id, manifest.Name, manifest.Version, false, true, null);
        }
        finally
        {
            if (Directory.Exists(staging)) Directory.Delete(staging, true);
        }
    }

    public async Task SetEnabledAsync(string extensionId, bool enabled, CancellationToken ct = default)
    {
        if (!AuraExtensionManifestValidator.IsSafeRelativeScriptPath(extensionId + ".js") || extensionId.Contains('/'))
            throw new ArgumentException("ID de extensão inválido", nameof(extensionId));
        string manifestPath = Path.Combine(_installedRoot, extensionId, "manifest.json");
        if (!File.Exists(manifestPath)) throw new FileNotFoundException("Extensão não instalada", extensionId);
        await _states.SetEnabledAsync(extensionId, enabled, ct);
        if (!enabled)
        {
            lock (_gate) _injected.RemoveWhere(key => key.StartsWith(extensionId + ":", StringComparison.Ordinal));
        }
    }

    public async Task InjectAsync(WebView view, string url, int tabId, CancellationToken ct = default)
    {
        if (!WebSecurityUrl(url, out Uri? pageUri) || !Directory.Exists(_installedRoot)) return;
        foreach (string packageRoot in Directory.EnumerateDirectories(_installedRoot))
        {
            ct.ThrowIfCancellationRequested();
            string manifestPath = Path.Combine(packageRoot, "manifest.json");
            if (!File.Exists(manifestPath)) continue;
            AuraExtensionManifest? manifest;
            try { manifest = JsonSerializer.Deserialize<AuraExtensionManifest>(await File.ReadAllTextAsync(manifestPath, ct), _json); }
            catch (Exception ex) { AURA.Mobile.AuraLog.Exception("Extension.Manifest", ex); continue; }
            AuraExtensionValidationResult validation = AuraExtensionManifestValidator.Validate(manifest);
            if (!validation.Valid || manifest is null || !_states.IsEnabled(manifest.Id) ||
                !manifest.Permissions.Contains(AuraExtensionPermissions.PageDom, StringComparer.Ordinal) ||
                !AuraExtensionOriginMatcher.IsAllowed(pageUri.AbsoluteUri, manifest)) continue;
            foreach (AuraContentScript script in manifest.ContentScripts)
            {
                ct.ThrowIfCancellationRequested();
                string scriptPath = Path.GetFullPath(Path.Combine(packageRoot, script.Path.Replace('/', Path.DirectorySeparatorChar)));
                string packageFull = Path.GetFullPath(packageRoot) + Path.DirectorySeparatorChar;
                if (!scriptPath.StartsWith(packageFull, StringComparison.Ordinal) || !File.Exists(scriptPath) || new FileInfo(scriptPath).Length > MaxScriptBytes) continue;
                string injectionKey = manifest.Id + ":" + script.Id + ":" + tabId + ":" + pageUri.AbsoluteUri;
                lock (_gate) if (!_injected.Add(injectionKey)) continue;
                try
                {
                    string wrapped = "(function(){\n" + await File.ReadAllTextAsync(scriptPath, ct) + "\n})();";
                    await MainThread.InvokeOnMainThreadAsync(() => view.EvaluateJavaScriptAsync(wrapped)).WaitAsync(ct);
                    AURA.Mobile.AuraLog.Info("Extension content.js executado: " + manifest.Id);
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex) { lock (_gate) _injected.Remove(injectionKey); AURA.Mobile.AuraLog.Exception("Extension.Inject", ex); }
            }
        }
    }

    private static async Task CopyBundledAsync(string asset, string destination, CancellationToken ct)
    {
        await using Stream source = await FileSystem.OpenAppPackageFileAsync(asset);
        await using FileStream target = File.Create(destination);
        await source.CopyToAsync(target, ct);
    }

    private static bool WebSecurityUrl(string raw, out Uri? uri)
    {
        uri = null;
        if (!WebSecurityPolicy.TryValidateHttpUrl(raw, out Uri validated, rejectLocalHost: true)) return false;
        uri = validated;
        return true;
    }
}
