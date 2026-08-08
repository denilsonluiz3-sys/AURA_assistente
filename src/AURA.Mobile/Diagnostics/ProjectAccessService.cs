using System.IO;
#if ANDROID
using Android.Content;
using Android.Net;
using Android.Provider;
#endif

namespace AURA.Mobile.Diagnostics;

/// <summary>
/// Ponte entre o agente e um projeto escolhido explicitamente pelo usuário.
/// O Android entrega uma URI persistente; a AURA mantém uma cópia de trabalho
/// privada para que as ferramentas existentes continuem usando caminhos locais
/// e sincroniza as alterações de volta ao projeto escolhido.
/// </summary>
public static class ProjectAccessService
{
    private const string UriPreference = "agent_project_tree_uri";
    private const string ProjectFolder = "project";

    public static string ProjectWorkspaceRoot =>
        Path.Combine(AgentWorkspace.WorkspaceRoot, ProjectFolder);

    public static bool IsLinked =>
        !string.IsNullOrWhiteSpace(Preferences.Default.Get(UriPreference, string.Empty));

    public static string StatusText => IsLinked
        ? "Projeto vinculado: " + Preferences.Default.Get("agent_project_name", "projeto")
        : "Nenhum projeto vinculado";

#if ANDROID
    public static async Task<bool> LinkAsync(CancellationToken ct = default)
    {
        var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity as AURA.Mobile.MainActivity;
        if (activity == null)
            throw new InvalidOperationException("Activity Android da AURA não está disponível.");

        Uri? uri = await activity.PickProjectDirectoryAsync(ct);
        if (uri == null)
            return false;

        var flags = ActivityFlags.GrantReadUriPermission | ActivityFlags.GrantWriteUriPermission;
        activity.ContentResolver.TakePersistableUriPermission(uri, flags);

        Preferences.Default.Set(UriPreference, uri.ToString());
        Preferences.Default.Set("agent_project_name",
            GetDisplayName(activity.ContentResolver, uri) ?? "projeto");

        await ImportTreeAsync(activity.ContentResolver, uri, ProjectWorkspaceRoot, ct);
        return true;
    }

    public static async Task<int> SyncBackAsync(CancellationToken ct = default)
    {
        string raw = Preferences.Default.Get(UriPreference, string.Empty);
        if (string.IsNullOrWhiteSpace(raw))
            return 0;

        var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity as AURA.Mobile.MainActivity;
        if (activity == null)
            throw new InvalidOperationException("Activity Android da AURA não está disponível.");

        Uri treeUri = Android.Net.Uri.Parse(raw)!;
        return await SyncDirectoryAsync(activity.ContentResolver, treeUri,
            ProjectWorkspaceRoot, treeUri, ct);
    }

    public static void Unlink()
    {
        Preferences.Default.Remove(UriPreference);
        Preferences.Default.Remove("agent_project_name");
        if (Directory.Exists(ProjectWorkspaceRoot))
            Directory.Delete(ProjectWorkspaceRoot, true);
    }

    private static async Task ImportTreeAsync(ContentResolver resolver, Uri treeUri,
        string localRoot, CancellationToken ct)
    {
        if (Directory.Exists(localRoot))
            Directory.Delete(localRoot, true);
        Directory.CreateDirectory(localRoot);

        string rootId = DocumentsContract.GetTreeDocumentId(treeUri)!;
        Uri rootUri = DocumentsContract.BuildDocumentUriUsingTree(treeUri, rootId)!;
        await ImportDirectoryAsync(resolver, treeUri, rootUri, localRoot, ct);
    }

    private static async Task ImportDirectoryAsync(ContentResolver resolver, Uri treeUri,
        Uri directoryUri, string localDirectory, CancellationToken ct)
    {
        Directory.CreateDirectory(localDirectory);
        foreach (var child in QueryChildren(resolver, treeUri,
                     DocumentsContract.GetDocumentId(directoryUri)!))
        {
            ct.ThrowIfCancellationRequested();
            string safeName = SanitizeName(child.Name);
            Uri childUri = DocumentsContract.BuildDocumentUriUsingTree(treeUri, child.Id)!;
            string localPath = Path.Combine(localDirectory, safeName);

            if (child.MimeType == DocumentsContract.Document.MimeTypeDir)
            {
                if (!ShouldIgnore(safeName))
                    await ImportDirectoryAsync(resolver, treeUri, childUri, localPath, ct);
            }
            else
            {
                if (ShouldIgnore(safeName)) continue;
                using Stream? input = resolver.OpenInputStream(childUri);
                if (input == null) continue;
                await using FileStream output = File.Create(localPath);
                await input.CopyToAsync(output, ct);
            }
        }
    }

    private static async Task<int> SyncDirectoryAsync(ContentResolver resolver, Uri treeUri,
        string localDirectory, Uri remoteDirectory, CancellationToken ct)
    {
        int count = 0;
        if (!Directory.Exists(localDirectory)) return 0;

        foreach (string localPath in Directory.EnumerateFileSystemEntries(localDirectory))
        {
            ct.ThrowIfCancellationRequested();
            string name = Path.GetFileName(localPath);
            if (ShouldIgnore(name)) continue;

            if (Directory.Exists(localPath))
            {
                Uri? childDir = FindChild(resolver, treeUri, remoteDirectory, name);
                if (childDir == null)
                {
                    childDir = DocumentsContract.CreateDocument(
                        resolver, remoteDirectory,
                        DocumentsContract.Document.MimeTypeDir, name);
                }
                if (childDir != null)
                    count += await SyncDirectoryAsync(
                        resolver, treeUri, localPath, childDir, ct);
                continue;
            }

            Uri? remoteFile = FindChild(resolver, treeUri, remoteDirectory, name);
            if (remoteFile == null)
            {
                remoteFile = DocumentsContract.CreateDocument(
                    resolver, remoteDirectory, GuessMimeType(name), name);
            }
            if (remoteFile == null) continue;

            await using FileStream input = File.OpenRead(localPath);
            using Stream? output = resolver.OpenOutputStream(remoteFile, "wt");
            if (output == null) continue;
            await input.CopyToAsync(output, ct);
            count++;
        }
        return count;
    }

    private static IEnumerable<DocumentEntry> QueryChildren(ContentResolver resolver,
        Uri treeUri, string parentId)
    {
        Uri childrenUri = DocumentsContract.BuildChildDocumentsUriUsingTree(treeUri, parentId)!;
        string[] projection =
        {
            DocumentsContract.Document.ColumnDocumentId,
            DocumentsContract.Document.ColumnDisplayName,
            DocumentsContract.Document.ColumnMimeType
        };

        using var cursor = resolver.Query(childrenUri, projection, null, null, null);
        if (cursor == null) yield break;

        int idCol = cursor.GetColumnIndex(DocumentsContract.Document.ColumnDocumentId);
        int nameCol = cursor.GetColumnIndex(DocumentsContract.Document.ColumnDisplayName);
        int mimeCol = cursor.GetColumnIndex(DocumentsContract.Document.ColumnMimeType);

        while (cursor.MoveToNext())
        {
            yield return new DocumentEntry(
                cursor.GetString(idCol) ?? string.Empty,
                cursor.GetString(nameCol) ?? "arquivo",
                cursor.GetString(mimeCol) ?? "application/octet-stream");
        }
    }

    private static Uri? FindChild(ContentResolver resolver, Uri treeUri,
        Uri parentUri, string name)
    {
        foreach (var child in QueryChildren(
                     resolver, treeUri, DocumentsContract.GetDocumentId(parentUri)!))
        {
            if (string.Equals(child.Name, name, StringComparison.Ordinal))
                return DocumentsContract.BuildDocumentUriUsingTree(treeUri, child.Id);
        }
        return null;
    }

    private static string? GetDisplayName(ContentResolver resolver, Uri uri)
    {
        string[] projection = { DocumentsContract.Document.ColumnDisplayName };
        using var cursor = resolver.Query(uri, projection, null, null, null);
        if (cursor != null && cursor.MoveToFirst())
            return cursor.GetString(0);
        return null;
    }
#endif

    private static bool ShouldIgnore(string name) =>
        name is ".git" or "bin" or "obj" or ".vs" or ".idea" or ".gradle";

    private static string SanitizeName(string name)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return string.IsNullOrWhiteSpace(name) ? "arquivo" : name;
    }

    private static string GuessMimeType(string name) => Path.GetExtension(name).ToLowerInvariant() switch
    {
        ".cs" or ".csproj" or ".sln" or ".slnx" or ".json" or ".xml" or ".yaml" or ".yml" or ".md" or ".txt" => "text/plain",
        ".png" => "image/png",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".zip" => "application/zip",
        _ => "application/octet-stream"
    };

    private readonly record struct DocumentEntry(string Id, string Name, string MimeType);
}
