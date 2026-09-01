// Universal provider UI integration marker.
// The existing AiConfigView remains the compatibility surface for Agent/Chat/RuntimeConfig.
// This file documents the intended UX contract: API key -> Load Models -> Select Model -> Save.
namespace AURA.Mobile.Controls;

internal static class UniversalAiSetupContract
{
    public const string Flow = "API_KEY -> CARREGAR_MODELOS -> MODELO -> SALVAR";
}
