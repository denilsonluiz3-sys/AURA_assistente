using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AURA.Memory;
using AURA.Mobile.Diagnostics;

namespace AURA.Mobile.Services;

/// <summary>
/// Memória procedural: reutiliza COMANDOS/ações bem-sucedidas, não prosa de chat.
/// Ordem: atalhos embutidos → SolutionStore (só se executável) → process-log (só se executável) → null.
/// Turnos de conversa NÃO são usados como atalho (evita só repetir a resposta anterior).
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
    /// string = ação reutilizável (preferencialmente com ```aura-sh``` para reexecutar).
    /// </summary>
    public string? TryResolveWithoutLlm(string userText)
    {
        if (string.IsNullOrWhiteSpace(userText))
            return null;

        string query = userText.Trim();

        try
        {
            // 0) Atalhos determinísticos (sem IA)
            string? shortcut = TryBuiltinShortcut(query);
            if (!string.IsNullOrWhiteSpace(shortcut))
            {
                AuraLog.Info("LocalPlaybook hit atalho embutido");
                return shortcut;
            }

            // 1) Memória procedural — só se a ação for executável (não prosa)
            var match = _solutions.FindBestMatch(query, threshold: 72);
            if (match != null && IsExecutableAction(match.ActionTaken))
            {
                AuraLog.Info("LocalPlaybook hit SolutionStore (executável): " + match.Id);
                string action = EnsureAuraShBlock(match.ActionTaken!);
                return "[memória procedural · reexecutar · sem IA]\n" + action;
            }

            // 2) process-log — só se a resposta gravada tiver ação executável
            string? fromLog = FindExecutableInProcessLog(query);
            if (!string.IsNullOrWhiteSpace(fromLog))
            {
                AuraLog.Info("LocalPlaybook hit process-log (executável)");
                RememberExecutable(query, fromLog!);
                return "[memória procedural · process-log · sem IA]\n" + EnsureAuraShBlock(fromLog!);
            }

            // Turnos de conversa deliberadamente NÃO são usados aqui:
            // isso só repetia a resposta anterior em vez de reexecutar o processo.

            return null;
        }
        catch (Exception ex)
        {
            AuraLog.Exception("LocalPlaybook.TryResolve", ex);
            return null;
        }
    }

    /// <summary>
    /// Frases curtas resolvidas localmente (sem LLM).
    /// </summary>
    internal static string? TryBuiltinShortcut(string query)
    {
        string q = query.Trim().ToLowerInvariant();
        if (q.Length == 0)
            return null;

        // ls / listar workspace / listar arquivos
        if (q is "ls" or "dir" or "listar" or "listar workspace" or "listar arquivos"
            or "listar arquivos do workspace" or "liste os arquivos" or "liste os arquivos do workspace")
        {
            return "[atalho · workspace · sem IA]\n```aura-sh\npwd\nls -la\n```";
        }

        // diagnóstico local (shell toybox — sem instalar nada)
        if (q is "diagnóstico" or "diagnostico" or "diagnosticar" or "diagnóstico do aparelho"
            or "diagnostico do aparelho" or "status do aparelho")
        {
            return "[atalho · diagnóstico · sem IA]\n```aura-sh\necho === modelo ===\ngetprop ro.product.model
echo === android ===\ngetprop ro.build.version.release
echo === sdk ===\ngetprop ro.build.version.sdk
echo === disco ===\ndf -h
echo === memória ===\ncat /proc/meminfo 2>/dev/null | head -n 5\n```";
        }

        // memória <query> / memoria <query> / buscar memória ...
        // Tratado no instância (precisa SolutionStore) — ver TryMemoryQuery

        return null;
    }

    /// <summary>Resolve "memória X" / "buscar memória X" via SolutionStore.</summary>
    private string? TryMemoryQuery(string query)
    {
        string q = query.Trim();
        var m = Regex.Match(q, @"^(?:mem[oó]ria|buscar\s+mem[oó]ria|search\s+memory)\s*[:\-]?\s*(.+)$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (!m.Success)
            return null;

        string topic = m.Groups[1].Value.Trim();
        if (topic.Length < 2)
            return "[atalho · memória]\nInforme o que buscar, ex.: memória listar workspace";

        try
        {
            var match = _solutions.FindBestMatch(topic, threshold: 60);
            if (match != null && IsExecutableAction(match.ActionTaken))
            {
                string action = EnsureAuraShBlock(match.ActionTaken!);
                return "[atalho · memória · reexecutar · sem IA]\n" +
                       "Query: " + topic + "\n" + action;
            }

            return "[atalho · memória · sem IA]\nNenhuma ação executável encontrada para: " + topic;
        }
        catch (Exception ex)
        {
            AuraLog.Exception("LocalPlaybook.TryMemoryQuery", ex);
            return null;
        }
    }

    /// <summary>
    /// Grava só ação executável. Prosa de chat é ignorada.
    /// </summary>
    public void RememberSuccess(string task, string actionTaken, string? details = null)
    {
        if (!IsExecutableAction(actionTaken))
        {
            AuraLog.Info("LocalPlaybook.RememberSuccess ignorado (não é ação executável)");
            return;
        }

        RememberExecutable(task, actionTaken!, details);
    }

    /// <summary>
    /// Preferência: bloco aura-sh da resposta; senão comandos run_shell da rodada.
    /// </summary>
    public void RememberFromRun(string task, IReadOnlyList<string>? shellCommands, string? answerText)
    {
        string? aura = ExtractAuraShell(answerText);
        if (!string.IsNullOrWhiteSpace(aura))
        {
            RememberExecutable(task, "```aura-sh\n" + aura.Trim() + "\n```");
            return;
        }

        if (shellCommands is { Count: > 0 })
        {
            var sb = new StringBuilder();
            sb.AppendLine("```aura-sh");
            foreach (string c in shellCommands)
            {
                if (!string.IsNullOrWhiteSpace(c))
                    sb.AppendLine(c.Trim());
            }
            sb.Append("```");
            RememberExecutable(task, sb.ToString());
            return;
        }

        AuraLog.Info("LocalPlaybook.RememberFromRun: nada executável para gravar");
    }

    private void RememberExecutable(string task, string actionTaken, string? details = null)
    {
        try
        {
            string action = EnsureAuraShBlock(actionTaken);
            _solutions.Record(task, action, details ?? string.Empty, success: true);
            AuraLog.Info("LocalPlaybook gravou ação procedural para: " + Shorten(task, 60));
        }
        catch (Exception ex)
        {
            AuraLog.Exception("LocalPlaybook.RememberExecutable", ex);
        }
    }

    /// <summary>True se parece comando/script reutilizável, não só texto de chat.</summary>
    public static bool IsExecutableAction(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        if (ExtractAuraShell(text) != null)
            return true;

        string t = text.Trim();
        string[] lines = t.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        int shellish = 0;
        foreach (string line in lines)
        {
            if (line.StartsWith("```", StringComparison.Ordinal))
                continue;
            if (LooksLikeShellLine(line))
                shellish++;
        }

        return shellish > 0 && shellish >= Math.Max(1, lines.Length / 3);
    }

    private static bool LooksLikeShellLine(string line)
    {
        if (line.Length < 2 || line.Length > 300)
            return false;

        if (line.Contains(' ') && line.Split(' ').Length > 12)
            return false;

        string[] starters =
        {
            "ls", "cd", "pwd", "cat", "echo", "grep", "find", "sed", "df", "du",
            "ps", "date", "getprop", "mkdir", "rm", "cp", "mv", "chmod", "head",
            "tail", "wc", "sh ", "sh\t", "./"
        };
        string lower = line.ToLowerInvariant();
        foreach (string s in starters)
        {
            if (lower.StartsWith(s, StringComparison.Ordinal) ||
                lower.StartsWith(s + " ", StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    private static string EnsureAuraShBlock(string action)
    {
        if (ExtractAuraShell(action) != null)
            return action.Trim();

        return "```aura-sh\n" + action.Trim() + "\n```";
    }

    private string? FindExecutableInProcessLog(string query)
    {
        foreach (string root in CandidateRoots())
        {
            string path = Path.Combine(root, "process-log.json");
            if (!File.Exists(path))
                continue;

            try
            {
                string raw = File.ReadAllText(path);
                string json = SanitizeProcessLogJson(raw);
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

                    if (string.IsNullOrWhiteSpace(prompt) || !IsExecutableAction(response))
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

    /// <summary>
    /// process-log no dispositivo às vezes começa com ',' ou lixo antes do '{'.
    /// Tenta recuperar; se impossível, devolve objeto vazio válido.
    /// </summary>
    internal static string SanitizeProcessLogJson(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return "{\"sessions\":[]}";

        string s = raw.Trim();

        // Remove BOM
        if (s.Length > 0 && s[0] == '\uFEFF')
            s = s[1..].Trim();

        // Pula lixo antes do primeiro '{'
        int brace = s.IndexOf('{');
        if (brace < 0)
            return "{\"sessions\":[]}";
        if (brace > 0)
            s = s[brace..].Trim();

        // Vírgulas soltas no início (caso clássico do log do aparelho)
        while (s.StartsWith(','))
            s = s[1..].Trim();

        if (!s.StartsWith('{'))
            return "{\"sessions\":[]}";

        try
        {
            using var doc = JsonDocument.Parse(s);
            if (doc.RootElement.ValueKind == JsonValueKind.Object)
                return s;
        }
        catch (JsonException)
        {
            // tenta fechar array/objeto truncado de forma conservadora
            try
            {
                string candidate = s.TrimEnd();
                if (!candidate.EndsWith('}'))
                    candidate += "]}";
                using var doc2 = JsonDocument.Parse(candidate);
                if (doc2.RootElement.ValueKind == JsonValueKind.Object)
                    return candidate;
            }
            catch
            {
                // ignore
            }
        }

        return "{\"sessions\":[]}";
    }

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

    private static string Shorten(string? text, int max)
    {
        if (string.IsNullOrWhiteSpace(text)) return "";
        text = text.Trim();
        return text.Length <= max ? text : text[..max] + "…";
    }

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
