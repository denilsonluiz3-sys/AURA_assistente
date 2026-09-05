using AURA.AI.UniversalAI;
using AURA.Abstractions.Orchestration;
using AURA.Abstractions.Process;
using AURA.Agents;
using AURA.Mobile.Speech;

namespace AURA.Mobile.Pages;

/// <summary>
/// Redirect residual → Agente. Chat removido do menu (Fase 3).
/// Stubs XAML; sem motor paralelo Intent/Orchestrator.
/// </summary>
public partial class ChatPage : ContentPage
{
    private bool _redirecting;

    public ChatPage(
        IUniversalAiClient client,
        AURA.Memory.MemoryStore memory,
        IProcessOrchestrator processEngine,
        IOrchestrator orchestrator,
        IIntentResolver intentResolver,
        ProcessRegistry processes,
        VoiceAssistantService? voice = null)
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

    private void OnSendClicked(object? sender, EventArgs e) { }
    private void OnProcessTapped(object? sender, TappedEventArgs e) { }
    private void OnProviderChanged(object? sender, EventArgs e) { }
}
