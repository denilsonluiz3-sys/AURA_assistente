using AURA.Memory;
using AURA.Mobile.Diagnostics;

namespace AURA.Mobile.Services;

/// <summary>
/// Camada fina sobre SolutionStore + MemoryStore: tenta resolver a tarefa sem LLM.
/// Ordem: (1) soluções procedurais, (2) pares pergunta/resposta já gravados, (3) null = precisa IA.
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
            // 1) Memória procedural (comandos/soluções bem-sucedidas)
            var match = _solutions.FindBestMatch(query, threshold: 72);
            if (match != null && !string.IsNullOrWhiteSpace(match.ActionTaken))
            {
                AuraLog.Info("LocalPlaybook hit SolutionStore: " + match.Id);
                return
                    "[memória local · sem IA]\n" +
                    match.ActionTaken.Trim() +
                    (string.IsNullOrWhiteSpace(match.ResultDetails)
                        ? string.Empty
                        : "\n\n—\n" + match.ResultDetails.Trim());
            }

            // 2) Turnos anteriores (pergunta do usuário ≈ comando atual → resposta do assistente)
            string? fromTurns = FindInConversationTurns(query);
            if (!string.IsNullOrWhiteSpace(fromTurns))
            {
                AuraLog.Info("LocalPlaybook hit MemoryStore turn");
                return "[memória local · conversa anterior · sem IA]\n" + fromTurns.Trim();
            }

            return null;
        }
        catch (Exception ex)
        {
            AuraLog.Exception("LocalPlaybook.TryResolve", ex);
            return null;
        }
    }

    /// <summary>
    /// Percorre turnos recentes: se um turno user for similar ao comando, devolve o próximo assistant.
    /// </summary>
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

            // Próximo turno assistant
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
                // se aparecer outro user antes, para
                if (string.Equals(next.Role, "user", StringComparison.OrdinalIgnoreCase))
                    break;
            }

            if (string.IsNullOrWhiteSpace(answer))
                continue;

            // Evita devolver respostas de erro/vazias
            if (answer.StartsWith("Erro:", StringComparison.OrdinalIgnoreCase)
                || answer.StartsWith("(sem texto", StringComparison.OrdinalIgnoreCase))
                continue;

            bestScore = score;
            bestAnswer = answer;
        }

        return bestAnswer;
    }

    /// <summary>Similaridade leve: Levenshtein se existir, senão overlap de palavras.</summary>
    private static int ScoreSimilarity(string a, string b)
    {
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
            return 0;
        if (a == b)
            return 100;

        // Reusa o Levenshtein do SolutionStore quando possível
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
