using System.Text.Json;

namespace AURA.Agents.Workgroups;

/// <summary>Persistência local de relatórios estruturados dos grupos de trabalho.</summary>
public sealed class AgentReportStore
{
    private readonly string _root;
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private readonly SemaphoreSlim _gate = new(1, 1);

    public AgentReportStore(string root)
    {
        if (string.IsNullOrWhiteSpace(root))
            throw new ArgumentException("Diretório dos relatórios obrigatório.", nameof(root));
        _root = Path.GetFullPath(root);
        Directory.CreateDirectory(_root);
    }

    public async Task SaveAsync(AgentReport report, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(report);
        if (string.IsNullOrWhiteSpace(report.Id))
            throw new ArgumentException("O relatório precisa de um Id.", nameof(report));

        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            string target = Path.Combine(_root, Sanitize(report.Id) + ".json");
            string temporary = target + ".tmp-" + Guid.NewGuid().ToString("N");
            await File.WriteAllTextAsync(temporary, JsonSerializer.Serialize(report, _json), ct).ConfigureAwait(false);
            File.Move(temporary, target, true);
        }
        finally { _gate.Release(); }
    }

    public AgentReport? Load(string id)
    {
        string path = Path.Combine(_root, Sanitize(id) + ".json");
        if (!File.Exists(path)) return null;
        try { return JsonSerializer.Deserialize<AgentReport>(File.ReadAllText(path), _json); }
        catch (JsonException) { return null; }
    }

    public IReadOnlyList<AgentReport> List()
    {
        var reports = new List<AgentReport>();
        foreach (string path in Directory.EnumerateFiles(_root, "*.json"))
        {
            try
            {
                var report = JsonSerializer.Deserialize<AgentReport>(File.ReadAllText(path), _json);
                if (report != null) reports.Add(report);
            }
            catch (JsonException) { }
        }
        return reports.OrderByDescending(x => x.CreatedAtUtc).ToArray();
    }

    private static string Sanitize(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Id obrigatório.", nameof(id));
        string value = new(id.Trim().Where(c => char.IsLetterOrDigit(c) || c is '-' or '_').ToArray());
        if (value.Length == 0 || value.Length > 100) throw new ArgumentException("Id inválido.", nameof(id));
        return value;
    }
}
