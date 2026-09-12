using System.Text.Json;

namespace AURA.Agents.Workgroups;

/// <summary>Persistência local do ciclo de vida dos trabalhos delegados.</summary>
public sealed class WorkItemStore
{
    private readonly string _root;
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private readonly SemaphoreSlim _gate = new(1, 1);

    public WorkItemStore(string root)
    {
        if (string.IsNullOrWhiteSpace(root))
            throw new ArgumentException("Diretório dos trabalhos obrigatório.", nameof(root));
        _root = Path.GetFullPath(root);
        Directory.CreateDirectory(_root);
    }

    public async Task SaveAsync(WorkItem item, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (string.IsNullOrWhiteSpace(item.Id))
            throw new ArgumentException("O trabalho precisa de um Id.", nameof(item));

        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            string target = Path.Combine(_root, Sanitize(item.Id) + ".json");
            string temporary = target + ".tmp-" + Guid.NewGuid().ToString("N");
            await File.WriteAllTextAsync(temporary, JsonSerializer.Serialize(item, _json), ct).ConfigureAwait(false);
            File.Move(temporary, target, true);
        }
        finally { _gate.Release(); }
    }

    public WorkItem? Load(string id)
    {
        string path = Path.Combine(_root, Sanitize(id) + ".json");
        if (!File.Exists(path)) return null;
        try { return JsonSerializer.Deserialize<WorkItem>(File.ReadAllText(path), _json); }
        catch (JsonException) { return null; }
    }

    public IReadOnlyList<WorkItem> List() => Directory.EnumerateFiles(_root, "*.json")
        .Select(path => TryLoad(path))
        .Where(item => item != null)
        .Cast<WorkItem>()
        .OrderByDescending(item => item.UpdatedAtUtc)
        .ToArray();

    private WorkItem? TryLoad(string path)
    {
        try { return JsonSerializer.Deserialize<WorkItem>(File.ReadAllText(path), _json); }
        catch (JsonException) { return null; }
    }

    private static string Sanitize(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Id obrigatório.", nameof(id));
        string value = new(id.Trim().Where(c => char.IsLetterOrDigit(c) || c is '-' or '_').ToArray());
        if (value.Length == 0 || value.Length > 100) throw new ArgumentException("Id inválido.", nameof(id));
        return value;
    }
}
