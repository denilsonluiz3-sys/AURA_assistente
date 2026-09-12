using AURA.Agents.Workgroups;

namespace AURA.Agents.Specialists;

/// <summary>Especialista inicial para planejar a análise do runtime de IA offline.</summary>
public sealed class OfflineAiPlanningAgent : IWorkGroupAgent
{
    public string GroupId => "ia-offline";

    public Task<AgentReport> AnalyzeAsync(
        WorkItem item,
        WorkGroupDefinition group,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new AgentReport
        {
            WorkItemId = item.Id,
            GroupId = group.Id,
            Objective = item.Objective,
            Status = AgentReportStatus.Complete,
            Findings = new[]
            {
                "A análise offline deve começar pela segurança e integridade do modelo importado.",
                "A inferência precisa ser validada antes de liberar tool calls locais.",
                "O runtime deve permanecer separado da lógica do AgentSession e do ToolRegistry."
            },
            Risks = new[]
            {
                "Biblioteca nativa ausente, incompatível ou compilada para ABI incorreta.",
                "Modelo GGUF inválido, incompleto ou fora do limite permitido.",
                "Tool call estruturada interpretada incorretamente pelo modelo local."
            },
            Evidence = new[]
            {
                new AgentEvidence
                {
                    Type = "offline-validation-plan",
                    Reference = "model-import",
                    Observation = "Verificar extensão GGUF, limite de tamanho, SHA-256, cópia atômica e proteção contra traversal.",
                    Confidence = 0.95
                },
                new AgentEvidence
                {
                    Type = "offline-validation-plan",
                    Reference = "native-runtime",
                    Observation = "Validar ABI arm64-v8a, carregamento da biblioteca, abertura do modelo, geração, cancelamento e liberação de memória.",
                    Confidence = 0.92
                },
                new AgentEvidence
                {
                    Type = "offline-validation-plan",
                    Reference = "tool-loop",
                    Observation = "Somente após texto simples funcionar, validar JSON de tool call, PolicyGuard, ferramenta autorizada e resposta final.",
                    Confidence = 0.94
                }
            },
            Recommendation = "Executar a validação em três fases: modelo, inferência simples e ciclo completo de ferramenta.",
            NextStep = "Preparar teste com um modelo GGUF real importado explicitamente pelo usuário.",
            Confidence = 0.93
        });
    }
}
