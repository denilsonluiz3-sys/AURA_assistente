using System.Security.Cryptography;
using System.Text.Json;
using AURA.Mobile.Diagnostics;
using Microsoft.Maui.Storage;

namespace AURA.Mobile.Services;

public sealed record AuraAttachmentRef(
    string Id,
    string FileName,
    string MimeType,
    long SizeBytes,
    string Sha256,
    string RelativePath,
    DateTimeOffset CreatedAtUtc);

/// <summary>
/// Armazena anexos selecionados explicitamente pelo usuário dentro do workspace.
/// O agente recebe somente referências relativas; nunca uma URL executável.
/// </summary>
public sealed class AttachmentStore
{
    private const long MaxBytes = 50L * 1024L * 1024L;
    private static readonly SemaphoreSlim RegistryGate = new(1, 1);
    private static readonly string[] AllowedExtensions =
    {
        ".txt", ".md", ".csv", ".json", ".xml", ".log", ".cs", ".js", ".py", ".sh",
        ".pdf", ".docx", ".xlsx", ".png", ".jpg", ".jpeg", ".webp", ".gif",
        ".mp3", ".wav", ".m4a", ".mp4", ".mov"
    };

    public async Task<AuraAttachmentRef> ImportAsync(FileResult file, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(file);
        string fileName = Path.GetFileName(file.FileName);
        string extension = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(fileName) || string.IsNullOrWhiteSpace(extension) ||
            !AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException("Tipo de arquivo não permitido para o workspace da AURA.");

        await using Stream source = await file.OpenReadAsync();
        if (source.CanSeek && source.Length > MaxBytes)
            throw new InvalidOperationException("O anexo excede o limite de 50 MB.");

        string root = AgentWorkspace.EnsureCreated();
        string attachmentRoot = Path.Combine(root, "attachments");
        Directory.CreateDirectory(attachmentRoot);
        string id = Guid.NewGuid().ToString("N");
        string storedName = id + extension.ToLowerInvariant();
        string destination = Path.Combine(attachmentRoot, storedName);
        string temporary = destination + ".tmp";
        bool committed = false;

        try
        {
            await using (FileStream target = new(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await source.CopyToAsync(target, cancellationToken);
                if (target.Length > MaxBytes)
                    throw new InvalidOperationException("O anexo excede o limite de 50 MB.");
            }

            string hash;
            await using (FileStream hashStream = File.OpenRead(temporary))
            {
                byte[] digest = await SHA256.HashDataAsync(hashStream, cancellationToken);
                hash = Convert.ToHexString(digest).ToLowerInvariant();
            }

            File.Move(temporary, destination, overwrite: false);
            committed = true;
            string relative = Path.GetRelativePath(root, destination).Replace(Path.DirectorySeparatorChar, '/');
            var item = new AuraAttachmentRef(
                id,
                fileName,
                string.IsNullOrWhiteSpace(file.ContentType) ? GuessMimeType(extension) : file.ContentType,
                new FileInfo(destination).Length,
                hash,
                relative,
                DateTimeOffset.UtcNow);

            await AppendRegistryAsync(root, item, cancellationToken);
            return item;
        }
        catch
        {
            TryDelete(temporary);
            // Se já foi publicado, preserva o anexo para uma nova tentativa de registro.
            if (!committed)
                TryDelete(destination);
            throw;
        }
    }

    public static string BuildAgentContext(IEnumerable<AuraAttachmentRef> attachments)
    {
        var list = attachments?.ToList() ?? new List<AuraAttachmentRef>();
        if (list.Count == 0) return string.Empty;

        var lines = new List<string>
        {
            "[ANEXOS SELECIONADOS PELO USUÁRIO]",
            "Os arquivos abaixo foram copiados para o workspace controlado da AURA.",
            "Use read_file apenas para arquivos textuais e não invente conteúdo que não tenha lido."
        };
        lines.AddRange(list.Select(a => $"- {a.RelativePath} | {a.FileName} | {a.MimeType} | {a.SizeBytes} bytes | sha256:{a.Sha256}"));
        return string.Join(Environment.NewLine, lines);
    }

    private static async Task AppendRegistryAsync(string root, AuraAttachmentRef item, CancellationToken cancellationToken)
    {
        await RegistryGate.WaitAsync(cancellationToken);
        try
        {
            string registry = Path.Combine(root, "attachments", "registry.json");
            string temporary = registry + ".tmp";
            List<AuraAttachmentRef> all = new();
            try
            {
                if (File.Exists(registry))
                {
                    await using FileStream input = File.OpenRead(registry);
                    all = await JsonSerializer.DeserializeAsync<List<AuraAttachmentRef>>(input, cancellationToken: cancellationToken) ?? new();
                }
            }
            catch (JsonException)
            {
                all = new();
            }

            all.RemoveAll(x => string.Equals(x.Id, item.Id, StringComparison.Ordinal));
            all.Add(item);
            try
            {
                await using (FileStream output = new(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
                    await JsonSerializer.SerializeAsync(output, all, new JsonSerializerOptions { WriteIndented = true }, cancellationToken);
                File.Move(temporary, registry, overwrite: true);
            }
            finally
            {
                TryDelete(temporary);
            }
        }
        finally
        {
            RegistryGate.Release();
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch { /* limpeza best-effort após falha de cópia */ }
    }

    private static string GuessMimeType(string extension) => extension.ToLowerInvariant() switch
    {
        ".txt" => "text/plain",
        ".md" => "text/markdown",
        ".csv" => "text/csv",
        ".json" => "application/json",
        ".pdf" => "application/pdf",
        ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        ".png" => "image/png",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".webp" => "image/webp",
        ".gif" => "image/gif",
        ".mp3" => "audio/mpeg",
        ".wav" => "audio/wav",
        ".m4a" => "audio/mp4",
        ".mp4" => "video/mp4",
        ".mov" => "video/quicktime",
        _ => "application/octet-stream"
    };
}
