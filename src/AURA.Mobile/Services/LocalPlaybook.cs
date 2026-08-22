using AURA.Memory;
using AURA.Mobile.Diagnostics;

namespace AURA.Mobile.Services;

/// <summary>
/// Camada fina sobre SolutionStore: tenta resolver a tarefa sem LLM.
/// Se houver match, devolve a ação gravada (texto ou script aura-sh).
/// </summary>
public sealed class LocalPlaybook
{
    private readonly SolutionStore _solutions;

    public LocalPlaybook(SolutionStore solutions)
    {
        _solutions = solutions ?? throw new ArgumentNullException(nameof(solutions));
    }

    /// <summary>
    /// null = precisa de IA ou ferramentas online.
    /// string = resposta/ação local reutilizável.
    /// </summary>
    public string? TryResolveWithoutLlm(string userText)
    {
        if (string.IsNullOrWhiteSpace(userText))
            return null;

        try
        {
            var match = _solutions.FindBestMatch(userText.Trim(), threshold: 78);
            if (match == null || string.IsNullOrWhiteSpace(match.ActionTaken))
                return null;

            AuraLog.Info("LocalPlaybook hit: " + match.Id + " score-task=" + match.TaskDescription);
            return
                "[playbook local · sem IA]\n" +
                match.ActionTaken.Trim() +
                (string.IsNullOrWhiteSpace(match.ResultDetails)
                    ? string.Empty
                    : "\n\n—\n" + match.ResultDetails.Trim());
        }
        catch (Exception ex)
        {
            AuraLog.Exception("LocalPlaybook.TryResolve", ex);
            return null;
        }
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
