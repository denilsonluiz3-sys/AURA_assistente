using System.Security.Cryptography;
using System.Text.Json;

namespace AURA.AI.UniversalAI;

/// <summary>Metadados de um modelo local aprovado pelo usuário.</summary>
public sealed class LocalModelDescriptor
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string Format { get; set; } = "gguf";
    public long SizeBytes { get; set; }
    public string Sha256 { get; set; } = string.Empty;
    public DateTime ImportedAtUtc { get; set; }
}

/// <summary>
/// Armazena modelos locais importados explicitamente pelo usuário.
/// Não baixa, executa ou substitui modelos automaticamente.
/// </summary>
public sealed class LocalModelStore
{
    private const long MaxModelBytes = 8L * 1024L * 1024L * 1024L;
    private readonly string _root;
    private readonly string _manifestPath;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public LocalModelStore(string root)
    {
        if (string.IsNullOrWhiteSpace(root))
            throw new ArgumentException("Diretório dos modelos obrigatório.", nameof(root));

        _root = Path.GetFullPath(root);
        _manifestPath = Path.Combine(_root, "models.json");
        Directory.CreateDirectory(_root);
    }

    public IReadOnlyList<LocalModelDescriptor> List()
    {
        _gate.Wait();
        try { return ReadManifest().ToArray(); }
        finally { _gate.Release(); }
    }

    public async Task<LocalModelDescriptor> ImportAsync(
        Stream source,
        LocalModelDescriptor descriptor,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(descriptor);

        string id = SanitizeId(descriptor.Id);
        string fileName = SanitizeFileName(descriptor.FileName);
        if (!fileName.EndsWith(".gguf", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Somente modelos GGUF são aceitos nesta fase.");

        string target = Path.Combine(_root, id + ".gguf");
        string temporary = target + ".importing-" + Guid.NewGuid().ToString("N");
        var imported = new LocalModelDescriptor
        {
            Id = id,
            DisplayName = string.IsNullOrWhiteSpace(descriptor.DisplayName) ? id : descriptor.DisplayName.Trim(),
            FileName = fileName,
            Format = "gguf",
            ImportedAtUtc = DateTime.UtcNow
        };

        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using (var output = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await source.CopyToAsync(output, ct).ConfigureAwait(false);
                await output.FlushAsync(ct).ConfigureAwait(false);
            }

            var info = new FileInfo(temporary);
            if (info.Length <= 0 || info.Length > MaxModelBytes)
                throw new InvalidDataException("Tamanho do modelo fora do limite permitido.");

            imported.SizeBytes = info.Length;
            imported.Sha256 = await ComputeSha256Async(temporary, ct).ConfigureAwait(false);
            File.Move(temporary, target, true);

            var manifest = ReadManifest()
                .Where(x => !string.Equals(x.Id, imported.Id, StringComparison.OrdinalIgnoreCase))
                .Append(imported)
                .OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                .ToList();
            WriteManifest(manifest);
            return imported;
        }
        finally
        {
            if (File.Exists(temporary))
                File.Delete(temporary);
            _gate.Release();
        }
    }

    public string GetModelPath(string id)
    {
        string safeId = SanitizeId(id);
        string path = Path.Combine(_root, safeId + ".gguf");
        if (!File.Exists(path))
            throw new FileNotFoundException("Modelo local não encontrado.", path);
        return path;
    }

    public bool Remove(string id)
    {
        _gate.Wait();
        try
        {
            string safeId = SanitizeId(id);
            string path = Path.Combine(_root, safeId + ".gguf");
            bool removed = File.Exists(path);
            if (removed) File.Delete(path);

            var remaining = ReadManifest()
                .Where(x => !string.Equals(x.Id, safeId, StringComparison.OrdinalIgnoreCase))
                .ToList();
            WriteManifest(remaining);
            return removed;
        }
        finally { _gate.Release(); }
    }

    private List<LocalModelDescriptor> ReadManifest()
    {
        if (!File.Exists(_manifestPath)) return new List<LocalModelDescriptor>();
        try
        {
            return JsonSerializer.Deserialize<List<LocalModelDescriptor>>(File.ReadAllText(_manifestPath))
                ?? new List<LocalModelDescriptor>();
        }
        catch (JsonException) { return new List<LocalModelDescriptor>(); }
    }

    private void WriteManifest(IReadOnlyList<LocalModelDescriptor> models)
    {
        string temporary = _manifestPath + ".tmp-" + Guid.NewGuid().ToString("N");
        File.WriteAllText(temporary, JsonSerializer.Serialize(models, new JsonSerializerOptions { WriteIndented = true }));
        File.Move(temporary, _manifestPath, true);
    }

    private static async Task<string> ComputeSha256Async(string path, CancellationToken ct)
    {
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        byte[] hash = await SHA256.HashDataAsync(stream, ct).ConfigureAwait(false);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string SanitizeId(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Id do modelo obrigatório.", nameof(value));
        string id = new(value.Trim().Where(c => char.IsLetterOrDigit(c) || c is '-' or '_' or '.').ToArray());
        if (id.Length is < 1 or > 80) throw new ArgumentException("Id do modelo inválido.", nameof(value));
        return id;
    }

    private static string SanitizeFileName(string value)
    {
        string name = Path.GetFileName(value ?? string.Empty);
        if (string.IsNullOrWhiteSpace(name) || name.Length > 180)
            throw new ArgumentException("Nome do arquivo do modelo inválido.", nameof(value));
        return name;
    }
}
