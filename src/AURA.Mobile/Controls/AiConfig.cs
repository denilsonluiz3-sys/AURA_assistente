using AURA.AI;

namespace AURA.Mobile.Controls;

/// <summary>
/// Fachada do painel de configuração da IA (AiConfigView) compartilhado entre
/// Chat e Agente. Mantém uma única instância; toda alteração persiste via
/// RuntimeConfig/Preferences e é aplicada no OpenRouterClient na hora.
/// </summary>
public static class AiConfig
{
    private static AiConfigView? _view;

    public static AiConfigView View => _view ??= new AiConfigView();

    public static void Load(OpenRouterClient client) => View.Load(client);

    public static void ApplyToClient() => View.ApplyToClient();
}