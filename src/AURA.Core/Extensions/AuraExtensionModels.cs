using System.Text.Json.Serialization;

namespace AURA.Core.Extensions;

public static class AuraExtensionPermissions
{
    public const string PageRead = "page.read";
    public const string PageDom = "page.dom";
    public const string ExtensionStorage = "extension.storage";

    public static readonly IReadOnlySet<string> Supported = new HashSet<string>(StringComparer.Ordinal)
    {
        PageRead, PageDom, ExtensionStorage
    };
}

public sealed class AuraExtensionManifest
{
    public int SchemaVersion { get; set; } = 1;
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public bool EnabledByDefault { get; set; }
    public List<string> Matches { get; set; } = new();
    public List<string> Exclude { get; set; } = new();
    public List<AuraContentScript> ContentScripts { get; set; } = new();
    public List<string> Permissions { get; set; } = new();
}

public sealed class AuraContentScript
{
    public string Id { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string RunAt { get; set; } = "document-end";
}

public sealed record AuraExtensionValidationResult(bool Valid, IReadOnlyList<string> Errors)
{
    public static AuraExtensionValidationResult Success() => new(true, Array.Empty<string>());
}

public static class AuraExtensionManifestValidator
{
    private static readonly System.Text.RegularExpressions.Regex IdRegex =
        new("^[a-z0-9]+(\\.[a-z0-9-]+)+$", System.Text.RegularExpressions.RegexOptions.CultureInvariant);
    private static readonly System.Text.RegularExpressions.Regex VersionRegex =
        new("^[0-9]+\\.[0-9]+\\.[0-9]+$", System.Text.RegularExpressions.RegexOptions.CultureInvariant);

    public static AuraExtensionValidationResult Validate(AuraExtensionManifest? manifest)
    {
        var errors = new List<string>();
        if (manifest is null) { errors.Add("manifesto ausente"); return new(false, errors); }
        if (manifest.SchemaVersion != 1) errors.Add("schemaVersion não suportado");
        if (!IdRegex.IsMatch(manifest.Id ?? string.Empty)) errors.Add("id inválido");
        if (string.IsNullOrWhiteSpace(manifest.Name) || manifest.Name.Length > 120) errors.Add("name inválido");
        if (!VersionRegex.IsMatch(manifest.Version ?? string.Empty)) errors.Add("version inválida");
        if (manifest.EnabledByDefault) errors.Add("extensões devem iniciar desativadas");
        if (manifest.Matches.Count == 0) errors.Add("matches não pode ser vazio");
        if (manifest.Matches.Any(pattern => !AuraExtensionOriginMatcher.TryParse(pattern, out _))) errors.Add("matches contém origem inválida");
        if (manifest.Exclude.Any(pattern => !AuraExtensionOriginMatcher.TryParse(pattern, out _))) errors.Add("exclude contém origem inválida");

        foreach (string permission in manifest.Permissions.Distinct(StringComparer.Ordinal))
            if (!AuraExtensionPermissions.Supported.Contains(permission)) errors.Add("permissão não suportada: " + permission);
        if (manifest.Permissions.Count != manifest.Permissions.Distinct(StringComparer.Ordinal).Count()) errors.Add("permissões duplicadas");

        var scriptIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (AuraContentScript script in manifest.ContentScripts)
        {
            if (string.IsNullOrWhiteSpace(script.Id) || !scriptIds.Add(script.Id)) errors.Add("content script id duplicado ou vazio");
            if (!IsSafeRelativeScriptPath(script.Path)) errors.Add("caminho de content script inválido: " + script.Path);
            if (script.RunAt is not ("document-start" or "document-end" or "document-idle")) errors.Add("runAt inválido: " + script.RunAt);
        }
        if (manifest.ContentScripts.Count == 0) errors.Add("contentScripts não pode ser vazio");
        return new(errors.Count == 0, errors);
    }

    public static bool IsSafeRelativeScriptPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || path.Length > 260 || Path.IsPathRooted(path) ||
            path.Contains("..", StringComparison.Ordinal) || path.Contains('\\') ||
            path.Length == 0 || path[0] == '/' || !path.EndsWith(".js", StringComparison.OrdinalIgnoreCase))
            return false;
        return path.Split('/').All(segment => segment.Length > 0 && segment != ".");
    }
}

public sealed record AuraExtensionOrigin(string Scheme, string Host, int Port, string PathPrefix);

public static class AuraExtensionOriginMatcher
{
    public static bool TryParse(string? pattern, out AuraExtensionOrigin origin)
    {
        origin = null!;
        if (string.IsNullOrWhiteSpace(pattern)) return false;
        string raw = pattern.Trim();
        if (raw.Contains('*', StringComparison.Ordinal) && !raw.EndsWith("/*", StringComparison.Ordinal)) return false;
        int wildcard = raw.IndexOf("/*", StringComparison.Ordinal);
        string uriText = wildcard >= 0 ? raw[..wildcard] : raw;
        if (!Uri.TryCreate(uriText, UriKind.Absolute, out Uri? uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) ||
            string.IsNullOrWhiteSpace(uri.Host) || !string.IsNullOrEmpty(uri.UserInfo) ||
            (uri.Port != -1 && uri.Port != 80 && uri.Port != 443) ||
            !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment)) return false;
        if (uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) || uri.Host.EndsWith(".local", StringComparison.OrdinalIgnoreCase)) return false;
        string pathPrefix = uri.AbsolutePath;
        if (wildcard >= 0 && !pathPrefix.EndsWith("/", StringComparison.Ordinal)) pathPrefix += "/";
        int port = uri.IsDefaultPort ? -1 : uri.Port;
        origin = new(uri.Scheme.ToLowerInvariant(), uri.Host.ToLowerInvariant(), port, pathPrefix.Length == 0 ? "/" : pathPrefix);
        return true;
    }

    public static bool IsMatch(string? url, AuraExtensionOrigin origin)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) ||
            !string.Equals(uri.Scheme, origin.Scheme, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(uri.Host, origin.Host, StringComparison.OrdinalIgnoreCase)) return false;
        int port = uri.IsDefaultPort ? -1 : uri.Port;
        if (port != origin.Port && !(origin.Port == -1 && (port == 80 || port == 443))) return false;
        return uri.AbsolutePath.StartsWith(origin.PathPrefix, StringComparison.Ordinal);
    }

    public static bool IsAllowed(string? url, AuraExtensionManifest manifest)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri)) return false;
        bool excluded = manifest.Exclude.Any(p => TryParse(p, out AuraExtensionOrigin? rule) && IsMatch(uri.AbsoluteUri, rule));
        return !excluded && manifest.Matches.Any(p => TryParse(p, out AuraExtensionOrigin? rule) && IsMatch(uri.AbsoluteUri, rule));
    }
}

public sealed class AuraExtensionStateStore
{
    private readonly string _root;
    public AuraExtensionStateStore(string root) => _root = root;

    public bool IsEnabled(string extensionId)
    {
        string path = StatePath(extensionId);
        return File.Exists(path) && string.Equals(File.ReadAllText(path).Trim(), "enabled", StringComparison.Ordinal);
    }

    public async Task SetEnabledAsync(string extensionId, bool enabled, CancellationToken ct = default)
    {
        Directory.CreateDirectory(_root);
        string temp = Path.Combine(_root, $".{extensionId}.{Guid.NewGuid():N}.tmp");
        await File.WriteAllTextAsync(temp, enabled ? "enabled" : "disabled", ct).ConfigureAwait(false);
        File.Move(temp, StatePath(extensionId), true);
    }

    private string StatePath(string id) => Path.Combine(_root, id + ".state");
}

public sealed class AuraExtensionStorage
{
    private const int MaxValueLength = 64 * 1024;
    private readonly string _root;
    public AuraExtensionStorage(string root) => _root = root;

    public async Task<string?> ReadAsync(string extensionId, string key, CancellationToken ct = default)
    {
        string path = Resolve(extensionId, key);
        return File.Exists(path) ? await File.ReadAllTextAsync(path, ct).ConfigureAwait(false) : null;
    }

    public async Task WriteAsync(string extensionId, string key, string value, CancellationToken ct = default)
    {
        if (value.Length > MaxValueLength) throw new InvalidOperationException("valor excede o limite da extensão");
        string path = Resolve(extensionId, key);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        string temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        await File.WriteAllTextAsync(temp, value, ct).ConfigureAwait(false);
        File.Move(temp, path, true);
    }

    public Task DeleteAsync(string extensionId, string key, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        string path = Resolve(extensionId, key);
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }

    private string Resolve(string extensionId, string key)
    {
        if (!IdRegex(extensionId) || string.IsNullOrWhiteSpace(key) || key.Contains('/') || key.Contains('\\') || key.Contains("..", StringComparison.Ordinal))
            throw new ArgumentException("identificador ou chave inválidos");
        string root = Path.GetFullPath(Path.Combine(_root, extensionId));
        string path = Path.GetFullPath(Path.Combine(root, key + ".json"));
        if (!path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal)) throw new UnauthorizedAccessException("storage fora da extensão");
        return path;
    }

    private static bool IdRegex(string id) => !string.IsNullOrWhiteSpace(id) && id.All(c => char.IsLetterOrDigit(c) || c is '.' or '-');
}
