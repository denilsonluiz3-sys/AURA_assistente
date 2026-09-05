namespace AURA.Mobile.Pages;

/// <summary>
/// Redirect residual → Agente. Chat removido do menu (Fase 3).
/// Stubs dos handlers do XAML só para compilar; sem motor paralelo.
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

    // Handlers exigidos pelo XAML — sem lógica (página só redireciona)
    private void OnSendClicked(object? sender, EventArgs e) { }
    private void OnProcessTapped(object? sender, TappedEventArgs e) { }
    private void OnProviderChanged(object? sender, EventArgs e) { }
}
