using AURA.Agents.Workgroups;

namespace AURA.Agents.Specialists;

/// <summary>
/// Especialista inicial de QA: produz um plano de validação sem afirmar que
/// testes ou builds foram executados.
/// </summary>
public sealed class QaCiPlanningAgent : IWorkGroupAgent
{
    public string GroupId => "qa-ci";

    public Task<AgentReport> AnalyzeAsync(
        WorkItem item,
        WorkGroupDefinition group,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string objective = item.Objective;
        var findings = new List<string>
        {
            "A solicitação foi classificada como trabalho de qualidade e validação.",
            "Nenhum build, teste ou APK é considerado validado apenas por este relatório."
        };
        var evidence = new List<AgentEvidence>
        {
            new()
            {
                Type = "validation-plan",
                Reference = "qa-ci",
                Observation = "Separar testes automatizados, build Android, inspeção do APK e validação funcional.",
                Confidence = 0.9
            }
        };

        if (ContainsAny(objective, "apk", "android", "nativo", "offline", "gguf"))
        {
            findings.Add("A validação Android deve incluir a ABI arm64-v8a e a presença das bibliotecas nativas esperadas.");
            evidence.Add(new AgentEvidence
            {
                Type = "validation-plan",
                Reference = "android-apk",
                Observation = "Verificar pacote, versão, permissões, libaura_llama.so e carregamento controlado do runtime.",
                Confidence = 0.85
            });
        }

        return Task.FromResult(new AgentReport
        {
            WorkItemId = item.Id,
            GroupId = group.Id,
            Objective = objective,
            Status = AgentReportStatus.Complete,
            Findings = findings,
            Evidence = evidence,
            Recommendation = "Executar a validação em etapas e registrar o resultado de cada uma antes de liberar a próxima.",
            NextStep = "Preparar checklist de testes específico para a alteração analisada.",
            Confidence = 0.88
        });
    }

    private static bool ContainsAny(string value, params string[] terms)
    {
        string normalized = value.ToLowerInvariant();
        return terms.Any(normalized.Contains);
    }
}
