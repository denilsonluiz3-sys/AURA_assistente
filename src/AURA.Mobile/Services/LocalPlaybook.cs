using AURA.Memory;
using AURA.Mobile.Diagnostics;

namespace AURA.Mobile.Services;

/// <summary>
/// Camada sobre SolutionStore: resolve tarefas sem LLM quando já houve êxito.
/// </summary>
public sealed class LocalPlaybook
{
    private readonly SolutionStore _solutions;

    public LocalPlaybook(SolutionStore solutions)
    {
        _solutions = solutions ?? throw new ArgumentNullException(nameof(solutions));
    }

    public string? TryResolveWithoutLlm(string userText)
    {
        if (string.IsNullOrWhiteSpace(userText))
            return null;

        try
        {
            var match = _solutions.FindBestMatch(userText.Trim(), threshold: 78);
            if (match == null || string.IsNullOrWhiteSpace(match.ActionTaken))
                return null;

            AuraLog.Info("LocalPlaybook hit: " + match.Id);
            string body = match.ActionTaken.Trim();
            if (!string.IsNullOrWhiteSpace(match.ResultDetails))
                body += "\n\n—\n" + match.ResultDetails.Trim();
            return "[playbook local · sem IA]\n" + body;
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
        string script = text[start..end].Trim();
        return string.IsNullOrWhiteSpace(script) ? null : script;
    }
}
