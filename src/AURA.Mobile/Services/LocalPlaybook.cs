using System.Text.Json;
using AURA.Memory;
using AURA.Mobile.Diagnostics;

namespace AURA.Mobile.Services;

/// <summary>
/// Resolve tarefas sem LLM, reutilizando o que já existe.
/// Ordem: SolutionStore → MemoryStore (turnos) → process-log.json no workspace → null (IA).
/// process-log.json é artefato opcional criado pelo agente; não é a memória oficial.
/// Hits nele são promovidos ao SolutionStore.
/// </summary>
public sealed class LocalPlaybook
{
    private readonly SolutionStore _solutions;
    private readonly MemoryStore? _memory;

    public LocalPlaybook(SolutionStore solutions, MemoryStore? memory = null)
    {
        _solutions = solutions ?? throw new ArgumentNullException(nameof(solutions));
        _memory = memory;
    }

    /// <summary>
    /// null = precisa de IA ou ferramentas online.
    /// string = resposta/ação local reutilizável.
    /// </summary>
    public string? TryResolveWithoutLlm(string userText)
    {
        if (string.IsNullOrWhiteSpace(userText))
            return null;

        string query = userText.Trim();

        try
        {
            // 1) Memória procedural oficial
            var match = _solutions.FindBestMatch(query, threshold: 72);
            if (match != null && !string.IsNullOrWhiteSpace(match.ActionTaken))
            {
                AuraLog.Info("LocalPlaybook hit SolutionStore: " + match.Id);
                return FormatLocal(match.ActionTaken, match.ResultDetails, "memória local · sem IA");
            }

            // 2) Turnos do MemoryStore (conversas do app)
            string? fromTurns = FindInConversationTurns(query);
            if (!string.IsNullOrWhiteSpace(fromTurns))
            {
                AuraLog.Info("LocalPlaybook hit MemoryStore turn");
                RememberSuccess(query, fromTurns!);
                return FormatLocal(fromTurns!, null, "memória local · conversa anterior · sem IA");
            }

            // 3) process-log.json no workspace (se o agente criou)
            string? fromLog = FindInProcessLog(query);
            if (!string.IsNullOrWhiteSpace(fromLog))
            {
                AuraLog.Info("LocalPlaybook hit process-log.json");
                RememberSuccess(query, fromLog!);
                return FormatLocal(fromLog!, null, "memória local · process-log · sem IA");
            }

            return null;
        }
        catch (Exception ex)
        {
            AuraLog.Exception("LocalPlaybook.TryResolve", ex);
            return null;
        }
    }

    private static string FormatLocal(string action, string? details, string tag)
    {
        string body = action.Trim();
        if (!string.IsNullOrWhiteSpace(details))
            body += "\n\n—\n" + details.Trim();
        return "[" + tag + "]\n" + body;
    }

    private string? FindInConversationTurns(string query)
    {
        if (_memory == null)
            return null;

        IReadOnlyList<MemoryEntry> entries;
        try
        {
            entries = _memory.Read(tail: 80);
        }
        catch
        {
            return null;
        }

        if (entries.Count == 0)
            return null;

        string q = query.ToLowerInvariant();
        string? bestAnswer = null;
        int bestScore = 0;

        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            if (e.Kind != MemoryKind.Turn)
                continue;
            if (!string.Equals(e.Role, "user", StringComparison.OrdinalIgnoreCase))
                continue;
            if (string.IsNullOrWhiteSpace(e.Text))
                continue;

            int score = ScoreSimilarity(q, e.Text.Trim().ToLowerInvariant());
            if (score < 70 || score <= bestScore)
                continue;

            string? answer = null;
            for (int j = i + 1; j < entries.Count && j <= i + 3; j++)
            {
                var next = entries[j];
                if (next.Kind != MemoryKind.Turn)
                    continue;
                if (string.Equals(next.Role, "assistant", StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrWhiteSpace(next.Text))
                {
                    answer = next.Text;
                    break;
                }
                if (string.Equals(next.Role, "user", StringComparison.OrdinalIgnoreCase))
                    break;
            }

            if (string.IsNullOrWhiteSpace(answer))
                continue;
            if (answer.StartsWith("Erro:", StringComparison.OrdinalIgnoreCase)
                || answer.StartsWith("(sem texto", StringComparison.OrdinalIgnoreCase))
                continue;

            bestScore = score;
            bestAnswer = answer;
        }

        return bestAnswer;
    }

    /// <summary>
    /// Lê process-log.json no workspace ativo (e fallback workspace privado).
    /// Formato esperado: { "sessions": [ { "prompt", "response", "status" } ] }
    /// </summary>
    private string? FindInProcessLog(string query)
    {
        foreach (string root in CandidateRoots())
        {
            string path = Path.Combine(root, "process-log.json");
            if (!File.Exists(path))
                continue;

            try
            {
                string json = File.ReadAllText(path);
                using var doc = JsonDocument.Parse(json);
                if (!doc.RootElement.TryGetProperty("sessions", out var sessions)
                    || sessions.ValueKind != JsonValueKind.Array)
                    continue;

                string q = query.ToLowerInvariant();
                string? best = null;
                int bestScore = 0;

                foreach (var session in sessions.EnumerateArray())
                {
                    string status = session.TryGetProperty("status", out var st)
                        ? (st.GetString() ?? "")
                        : "completed";
                    if (status.Equals("failed", StringComparison.OrdinalIgnoreCase))
                        continue;

                    string prompt = session.TryGetProperty("prompt", out var p)
                        ? (p.GetString() ?? "")
                        : "";
                    string response = session.TryGetProperty("response", out var r)
                        ? (r.GetString() ?? "")
                        : "";

                    if (string.IsNullOrWhiteSpace(prompt) || string.IsNullOrWhiteSpace(response))
                        continue;

                    int score = ScoreSimilarity(q, prompt.Trim().ToLowerInvariant());
                    if (score < 70 || score <= bestScore)
                        continue;

                    bestScore = score;
                    best = response;
                }

                if (!string.IsNullOrWhiteSpace(best))
                    return best;
            }
            catch (Exception ex)
            {
                AuraLog.Info("LocalPlaybook process-log skip " + path + ": " + ex.Message);
            }
        }

        return null;
    }

    /// <summary>Lista de raízes candidatas — sem yield dentro de try/catch (CS1626).</summary>
    private static List<string> CandidateRoots()
    {
        var roots = new List<string>();

        string active = string.Empty;
        try { active = AgentWorkspace.ActiveRoot ?? string.Empty; }
        catch { /* ignore */ }
        if (!string.IsNullOrWhiteSpace(active))
            roots.Add(active);

        string privateWs = string.Empty;
        try { privateWs = AgentWorkspace.WorkspaceRoot ?? string.Empty; }
        catch { /* ignore */ }
        if (!string.IsNullOrWhiteSpace(privateWs)
            && !string.Equals(privateWs, active, StringComparison.OrdinalIgnoreCase))
            roots.Add(privateWs);

        try
        {
            string app = FileSystem.AppDataDirectory ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(app)
                && !string.Equals(app, active, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(app, privateWs, StringComparison.OrdinalIgnoreCase))
                roots.Add(app);
        }
        catch { /* ignore */ }

        return roots;
    }

    private static int ScoreSimilarity(string a, string b)
    {
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
            return 0;
        if (a == b)
            return 100;

        try
        {
            return Levenshtein.SimilarityPercent(a, b);
        }
        catch
        {
            // fallback por palavras
        }

        var stop = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "o","a","os","as","um","uma","de","do","da","em","com","para","por","e","que","me","mostrar","mostre"
        };
        var wa = a.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 2 && !stop.Contains(w)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var wb = b.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 2 && !stop.Contains(w)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (wa.Count == 0 || wb.Count == 0)
            return 0;
        int inter = wa.Intersect(wb, StringComparer.OrdinalIgnoreCase).Count();
        int union = wa.Union(wb, StringComparer.OrdinalIgnoreCase).Count();
        return union == 0 ? 0 : (int)(100.0 * inter / union);
    }

    public void RememberSuccess(string task, string actionTaken, string? details = null)
    {
        try
        {
            _solutions.Record(task, actionTaken, details ?? string.Empty, success: true);
        }
        catch (Exception ex)
        {
            AuraLog.Exception("LocalPlaybook.RememberSuccess", ex);
        }
    }

    /// <summary>Extrai bloco ```aura-sh ... ``` se a IA (ou playbook) devolver script.</summary>
    public static string? ExtractAuraShell(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        const string open = "```aura-sh";
        int i = text.IndexOf(open, StringComparison.OrdinalIgnoreCase);
        if (i < 0) return null;
        int start = text.IndexOf('\n', i);
        if (start < 0) return null;
        start++;
        int end = text.IndexOf("```", start, StringComparison.Ordinal);
        if (end < 0) return null;
        return text[start..end].Trim();
    }
}
