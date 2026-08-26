using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AURA.Memory;
using AURA.Mobile.Diagnostics;

namespace AURA.Mobile.Services;

/// <summary>
/// Memória procedural: reutiliza COMANDOS/ações bem-sucedidas, não prosa de chat.
/// Ordem sem IA: atalhos embutidos → "memória X" explícito → null (deixa o agente seguir).
/// NÃO faz match automático do SolutionStore em qualquer frase — isso sequestrava
/// a conversa e só relistava o workspace.
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
    /// null = seguir fluxo normal (agente / IA).
    /// string = ação local determinística.
    /// </summary>
    public string? TryResolveWithoutLlm(string userText)
    {
        if (string.IsNullOrWhiteSpace(userText))
            return null;

        string query = userText.Trim();

        // "continue" / prosa longa nunca são atalho de memória procedural
        if (IsContinueLike(query) || LooksLikeConversation(query))
            return null;

        try
        {
            // 0) Atalhos determinísticos (sem IA)
            string? shortcut = TryBuiltinShortcut(query);
            if (!string.IsNullOrWhiteSpace(shortcut))
            {
                AuraLog.Info("LocalPlaybook hit atalho embutido");
                return shortcut;
            }

            // 0b) só quando o usuário pede explicitamente: memória <query>
            string? memHit = TryMemoryQuery(query);
            if (!string.IsNullOrWhiteSpace(memHit))
            {
                AuraLog.Info("LocalPlaybook hit atalho memória");
                return memHit;
            }

            // Auto-match SolutionStore / process-log DESLIGADO:
            // frases parecidas (threshold baixo) só reexecutavam ls e bloqueavam a tarefa.
            return null;
        }
        catch (Exception ex)
        {
            AuraLog.Exception("LocalPlaybook.TryResolve", ex);
            return null;
        }
    }

    private static bool IsContinueLike(string query)
    {
        string l = query.Trim().ToLowerInvariant();
        return l is "continue" or "continua" or "continuar" or "prosseguir"
            || l.StartsWith("continue ", StringComparison.Ordinal)
            || l.StartsWith("continua ", StringComparison.Ordinal)
            || l.StartsWith("continuar ", StringComparison.Ordinal);
    }

    /// <summary>Perguntas / pedidos longos não devem ser sequestrados por memória.</summary>
    private static bool LooksLikeConversation(string query)
    {
        if (query.Length > 80)
            return true;
        if (query.Contains('?'))
            return true;
        // várias frases
        if (query.Count(c => c == '.' || c == '!' || c == '\n') >= 2)
            return true;
        return false;
    }

    /// <summary>
    /// Frases curtas resolvidas localmente (sem LLM).
    /// </summary>
    internal static string? TryBuiltinShortcut(string query)
    {
        string q = query.Trim().ToLowerInvariant();
        if (q.Length == 0)
            return null;

        if (q is "ls" or "dir" or "listar" or "listar workspace" or "listar arquivos"
            or "listar arquivos do workspace" or "liste os arquivos" or "liste os arquivos do workspace")
        {
            return "[atalho · workspace · sem IA]\n```aura-sh\npwd\nls -la\n```";
        }

        if (q is "diagnóstico" or "diagnostico" or "diagnosticar" or "diagnóstico do aparelho"
            or "diagnostico do aparelho" or "status do aparelho")
        {
            return
                "[atalho · diagnóstico · sem IA]\n" +
                "```aura-sh\n" +
                "echo === modelo ===\n" +
                "getprop ro.product.model\n" +
                "echo === android ===\n" +
                "getprop ro.build.version.release\n" +
                "echo === sdk ===\n" +
                "getprop ro.build.version.sdk\n" +
                "echo === disco ===\n" +
                "df -h\n" +
                "echo === memória ===\n" +
                "cat /proc/meminfo 2>/dev/null | head -n 5\n" +
                "```";
        }

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
            // threshold alto: só reexecuta se for claramente o mesmo pedido
            var match = _solutions.FindBestMatch(topic, threshold: 88);
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

    public void RememberSuccess(string task, string actionTaken, string? details = null)
    {
        if (!IsExecutableAction(actionTaken))
        {
            AuraLog.Info("LocalPlaybook.RememberSuccess ignorado (não é ação executável)");
            return;
        }

        if (!ShouldRememberTask(task))
        {
            AuraLog.Info("LocalPlaybook.RememberSuccess ignorado (tarefa conversacional)");
            return;
        }

        RememberExecutable(task, actionTaken!, details);
    }

    public void RememberFromRun(string task, IReadOnlyList<string>? shellCommands, string? answerText)
    {
        if (!ShouldRememberTask(task))
        {
            AuraLog.Info("LocalPlaybook.RememberFromRun: tarefa não memorizável");
            return;
        }

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

    /// <summary>Não associa ls a "perfeito!!!" / perguntas longas.</summary>
    private static bool ShouldRememberTask(string? task)
    {
        if (string.IsNullOrWhiteSpace(task))
            return false;
        string t = task.Trim();
        if (t.Length < 3 || t.Length > 120)
            return false;
        if (IsContinueLike(t))
            return false;
        if (LooksLikeConversation(t))
            return false;
        // só memoriza se parecer pedido de ação curta
        string l = t.ToLowerInvariant();
        string[] ok =
        {
            "ls", "listar", "dir", "diagnóst", "diagnost", "status", "df", "pwd",
            "memória", "memoria", "cat ", "grep", "find ", "echo "
        };
        foreach (string k in ok)
        {
            if (l.Contains(k, StringComparison.Ordinal))
                return true;
        }
        // comando shell curto
        if (t.Length <= 40 && !t.Contains('?') && t.Split(' ').Length <= 6)
            return true;
        return false;
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

    internal static string SanitizeProcessLogJson(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return "{\"sessions\":[]}";

        string s = raw.Trim();

        if (s.Length > 0 && s[0] == '\uFEFF')
            s = s[1..].Trim();

        int brace = s.IndexOf('{');
        if (brace < 0)
            return "{\"sessions\":[]}";
        if (brace > 0)
            s = s[brace..].Trim();

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
