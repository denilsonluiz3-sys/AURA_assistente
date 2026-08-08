using System.IO;
#if ANDROID
using Android.Content;
using AndroidUri = Android.Net.Uri;
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
    private const string AndroidUriPreference = "agent_project_tree_uri";
    private const string ProjectFolder = "project";

    public static string ProjectWorkspaceRoot =>
        Path.Combine(AgentWorkspace.WorkspaceRoot, ProjectFolder);

    public static bool IsLinked =>
        !string.IsNullOrWhiteSpace(Preferences.Default.Get(AndroidUriPreference, string.Empty));

    public static string StatusText => IsLinked
        ? "Projeto vinculado: " + Preferences.Default.Get("agent_project_name", "projeto")
        : "Nenhum projeto vinculado";

#if ANDROID
    public static async Task<bool> LinkAsync(CancellationToken ct = default)
    {
        var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity as AURA.Mobile.MainActivity;
        if (activity == null)
            throw new InvalidOperationException("Activity Android da AURA não está disponível.");

        AndroidUri? uri = await activity.PickProjectDirectoryAsync(ct);
        if (uri == null)
            return false;

        var flags = ActivityFlags.GrantReadAndroidUriPermission | ActivityFlags.GrantWriteAndroidUriPermission;
        activity.ContentResolver.TakePersistableAndroidUriPermission(uri, flags);

        Preferences.Default.Set(AndroidUriPreference, uri.ToString());
        Preferences.Default.Set("agent_project_name",
            GetDisplayName(activity.ContentResolver, uri) ?? "projeto");

        await ImportTreeAsync(activity.ContentResolver, uri, ProjectWorkspaceRoot, ct);
        return true;
    }

    public static async Task<int> SyncBackAsync(CancellationToken ct = default)
    {
        string raw = Preferences.Default.Get(AndroidUriPreference, string.Empty);
        if (string.IsNullOrWhiteSpace(raw))
            return 0;

        var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity as AURA.Mobile.MainActivity;
        if (activity == null)
            throw new InvalidOperationException("Activity Android da AURA não está disponível.");

        AndroidUri treeAndroidUri = Android.Net.AndroidUri.Parse(raw)!;
        return await SyncDirectoryAsync(activity.ContentResolver, treeAndroidUri,
            ProjectWorkspaceRoot, treeAndroidUri, ct);
    }

    public static void Unlink()
    {
        Preferences.Default.Remove(AndroidUriPreference);
        Preferences.Default.Remove("agent_project_name");
        if (Directory.Exists(ProjectWorkspaceRoot))
            Directory.Delete(ProjectWorkspaceRoot, true);
    }

    private static async Task ImportTreeAsync(ContentResolver resolver, AndroidUri treeAndroidUri,
        string localRoot, CancellationToken ct)
    {
        if (Directory.Exists(localRoot))
            Directory.Delete(localRoot, true);
        Directory.CreateDirectory(localRoot);

        string rootId = DocumentsContract.GetTreeDocumentId(treeAndroidUri)!;
        AndroidUri rootAndroidUri = DocumentsContract.BuildDocumentAndroidUriUsingTree(treeAndroidUri, rootId)!;
        await ImportDirectoryAsync(resolver, treeAndroidUri, rootAndroidUri, localRoot, ct);
    }

    private static async Task ImportDirectoryAsync(ContentResolver resolver, AndroidUri treeAndroidUri,
        AndroidUri directoryAndroidUri, string localDirectory, CancellationToken ct)
    {
        Directory.CreateDirectory(localDirectory);
        foreach (var child in QueryChildren(resolver, treeAndroidUri,
                     DocumentsContract.GetDocumentId(directoryAndroidUri)!))
        {
            ct.ThrowIfCancellationRequested();
            string safeName = SanitizeName(child.Name);
            AndroidUri childAndroidUri = DocumentsContract.BuildDocumentAndroidUriUsingTree(treeAndroidUri, child.Id)!;
            string localPath = Path.Combine(localDirectory, safeName);

            if (child.MimeType == DocumentsContract.Document.MimeTypeDir)
            {
                if (!ShouldIgnore(safeName))
                    await ImportDirectoryAsync(resolver, treeAndroidUri, childAndroidUri, localPath, ct);
            }
            else
            {
                if (ShouldIgnore(safeName)) continue;
                using Stream? input = resolver.OpenInputStream(childAndroidUri);
                if (input == null) continue;
                await using FileStream output = File.Create(localPath);
                await input.CopyToAsync(output, ct);
            }
        }
    }

    private static async Task<int> SyncDirectoryAsync(ContentResolver resolver, AndroidUri treeAndroidUri,
        string localDirectory, AndroidUri remoteDirectory, CancellationToken ct)
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
                AndroidUri? childDir = FindChild(resolver, treeAndroidUri, remoteDirectory, name);
                if (childDir == null)
                {
                    childDir = DocumentsContract.CreateDocument(
                        resolver, remoteDirectory,
                        DocumentsContract.Document.MimeTypeDir, name);
                }
                if (childDir != null)
                    count += await SyncDirectoryAsync(
                        resolver, treeAndroidUri, localPath, childDir, ct);
                continue;
            }

            AndroidUri? remoteFile = FindChild(resolver, treeAndroidUri, remoteDirectory, name);
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
        AndroidUri treeAndroidUri, string parentId)
    {
        AndroidUri childrenAndroidUri = DocumentsContract.BuildChildDocumentsAndroidUriUsingTree(treeAndroidUri, parentId)!;
        string[] projection =
        {
            DocumentsContract.Document.ColumnDocumentId,
            DocumentsContract.Document.ColumnDisplayName,
            DocumentsContract.Document.ColumnMimeType
        };

        using var cursor = resolver.Query(childrenAndroidUri, projection, null, null, null);
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

    private static AndroidUri? FindChild(ContentResolver resolver, AndroidUri treeAndroidUri,
        AndroidUri parentAndroidUri, string name)
    {
        foreach (var child in QueryChildren(
                     resolver, treeAndroidUri, DocumentsContract.GetDocumentId(parentAndroidUri)!))
        {
            if (string.Equals(child.Name, name, StringComparison.Ordinal))
                return DocumentsContract.BuildDocumentAndroidUriUsingTree(treeAndroidUri, child.Id);
        }
        return null;
    }

    private static string? GetDisplayName(ContentResolver resolver, AndroidUri uri)
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
