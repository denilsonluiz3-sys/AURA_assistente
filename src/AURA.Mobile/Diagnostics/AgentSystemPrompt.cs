using AURA.AI;

namespace AURA.Mobile.Diagnostics;

/// <summary>
/// Prompt do Agente na UI. Delega regras de shell ao núcleo (DefaultAgentSystemPrompt)
/// para não divergir do AgentSession.
/// </summary>
public static class AgentSystemPrompt
{
    public static string Build() => DefaultAgentSystemPrompt.Merge(
        "Você é o agente de arquivos e execução da AURA no Android.\n" +
        "Use as ferramentas registradas; se uma falhar, tente alternativa ou reporte.\n" +
        "Hardware: use a tool android para battery, device, network, apps, app_find, app_launch, clipboard, etc.\n" +
        "run_executor para git/python/node quando disponíveis; list_programs/run_program para automações.\n");
}
