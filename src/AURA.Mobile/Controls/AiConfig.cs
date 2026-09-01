using AURA.AI.UniversalAI;

namespace AURA.Mobile.Controls;

/// <summary>
/// Fachada do painel de configuração da IA (AiConfigView) compartilhado entre
/// Chat e Agente. Mantém uma única instância; toda alteração persiste via
/// RuntimeConfig/Preferences e é aplicada no IUniversalAiClient na hora.
/// </summary>
public static class AiConfig
{
    private static AiConfigView? _view;

    public static AiConfigView View => _view ??= new AiConfigView();

    public static void Load(IUniversalAiClient? client = null) => View.Load(client);

    public static void ApplyToClient() => View.ApplyToClient();
}
