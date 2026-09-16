using System.Text.Json;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using AURA.Core.Extensions;
using AURA.Core.Security;

namespace AURA.Mobile.Extensions;

/// <summary>
/// Executa content scripts próprios somente no BrowserPage. Não é usado pelo Web AI.
/// A ausência de estado enabled mantém extensões desativadas por padrão.
/// </summary>
public sealed class BrowserExtensionCoordinator
{
    private const int MaxScriptBytes = 256 * 1024;
    private readonly string _installedRoot = Path.Combine(FileSystem.AppDataDirectory, "extensions", "installed");
    private readonly AuraExtensionStateStore _states;
    private readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };
    private readonly HashSet<string> _injected = new(StringComparer.Ordinal);
    private readonly object _gate = new();

    public BrowserExtensionCoordinator()
    {
        _states = new AuraExtensionStateStore(Path.Combine(FileSystem.AppDataDirectory, "extensions", "state"));
    }

    public async Task InjectAsync(WebView view, string url, int tabId, CancellationToken ct = default)
    {
        if (!WebSecurityUrl(url, out Uri? pageUri)) return;
        if (!Directory.Exists(_installedRoot)) return;

        foreach (string packageRoot in Directory.EnumerateDirectories(_installedRoot))
        {
            ct.ThrowIfCancellationRequested();
            string manifestPath = Path.Combine(packageRoot, "manifest.json");
            if (!File.Exists(manifestPath)) continue;

            AuraExtensionManifest? manifest;
            try
            {
                string raw = await File.ReadAllTextAsync(manifestPath, ct).ConfigureAwait(false);
                manifest = JsonSerializer.Deserialize<AuraExtensionManifest>(raw, _json);
            }
            catch (Exception ex)
            {
                AURA.Mobile.AuraLog.Exception("Extension.Manifest", ex);
                continue;
            }

            AuraExtensionValidationResult validation = AuraExtensionManifestValidator.Validate(manifest);
            if (!validation.Valid || manifest is null || !_states.IsEnabled(manifest.Id) ||
                !manifest.Permissions.Contains(AuraExtensionPermissions.PageDom, StringComparer.Ordinal) ||
                !AuraExtensionOriginMatcher.IsAllowed(pageUri.AbsoluteUri, manifest))
                continue;

            foreach (AuraContentScript script in manifest.ContentScripts)
            {
                ct.ThrowIfCancellationRequested();
                string scriptPath = Path.GetFullPath(Path.Combine(packageRoot, script.Path.Replace('/', Path.DirectorySeparatorChar)));
                string packageFull = Path.GetFullPath(packageRoot) + Path.DirectorySeparatorChar;
                if (!scriptPath.StartsWith(packageFull, StringComparison.Ordinal) || !File.Exists(scriptPath)) continue;
                FileInfo info = new(scriptPath);
                if (info.Length > MaxScriptBytes) continue;

                string injectionKey = manifest.Id + ":" + script.Id + ":" + tabId + ":" + pageUri.AbsoluteUri;
                lock (_gate)
                {
                    if (!_injected.Add(injectionKey)) continue;
                }

                string content = await File.ReadAllTextAsync(scriptPath, ct).ConfigureAwait(false);
                string wrapped = "(function(){\n" + content + "\n})();";
                try
                {
                    await MainThread.InvokeOnMainThreadAsync(() => view.EvaluateJavaScriptAsync(wrapped)).WaitAsync(ct).ConfigureAwait(false);
                    AURA.Mobile.AuraLog.Info("Extension content.js executado: " + manifest.Id);
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    lock (_gate) _injected.Remove(injectionKey);
                    AURA.Mobile.AuraLog.Exception("Extension.Inject", ex);
                }
            }
        }
    }

    private static bool WebSecurityUrl(string raw, out Uri? uri)
    {
        uri = null;
        if (!WebSecurityPolicy.TryValidateHttpUrl(raw, out Uri validated, rejectLocalHost: true)) return false;
        uri = validated;
        return true;
    }
}
