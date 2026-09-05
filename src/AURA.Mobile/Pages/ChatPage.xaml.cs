namespace AURA.Mobile.Pages;

/// <summary>
/// Redirect residual → Agente. Chat removido do menu (Fase 3).
/// Mantido no DI para não quebrar registro; sem motor paralelo.
/// </summary>
public partial class ChatPage : ContentPage
{
    private bool _redirecting;

    public ChatPage(
        AURA.AI.UniversalAI.IUniversalAiClient client,
        AURA.Memory.MemoryStore memory,
        AURA.Abstractions.Process.IProcessOrchestrator processEngine,
        AURA.Abstractions.Orchestration.IOrchestrator orchestrator,
        AURA.Abstractions.IIntentResolver intentResolver,
        ProcessRegistry processes,
        Speech.VoiceAssistantService? voice = null)
    {
        InitializeComponent();
        // Parâmetros mantidos só para assinatura DI compatível
        _ = (client, memory, processEngine, orchestrator, intentResolver, processes, voice);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_redirecting) return;
        _redirecting = true;
        try
        {
            if (Application.Current?.Windows?.FirstOrDefault()?.Page is MainPage main)
                await main.NavigateToProcessAsync("Agente");

            if (Navigation.NavigationStack.Count > 1 &&
                ReferenceEquals(Navigation.NavigationStack[^1], this))
                await Navigation.PopAsync(false);
        }
        catch (Exception ex)
        {
            AuraLog.Exception("ChatPage.Redirect", ex);
        }
        finally
        {
            _redirecting = false;
        }
    }
}
