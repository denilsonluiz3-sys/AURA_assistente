namespace AURA.Mobile.Pages;

/// <summary>
/// Ecossistema AURA: visualização do núcleo operacional (chat, agentes,
/// células, runtime, shell, python, git) com navegação direta para cada
/// módulo real do app. Nada de HTML externo: tudo nativo, seguindo o
/// design system do app.
/// </summary>
public partial class EcosystemPage : ContentPage
{
    private readonly ChatPage _chat;
    private readonly AgentPage _agent;
    private readonly CellsPage _cells;
    private readonly RunPage _run;
    private readonly TerminalPage _terminal;
    private readonly ExecutorsPage _executors;

    public EcosystemPage(ChatPage chat, AgentPage agent, CellsPage cells,
        RunPage run, TerminalPage terminal, ExecutorsPage executors)
    {
        InitializeComponent();
        _chat = chat;
        _agent = agent;
        _cells = cells;
        _run = run;
        _terminal = terminal;
        _executors = executors;

        BindableLayout.SetItemsSource(ModulesHost, BuildModules());
    }

    private List<EcosystemModule> BuildModules() => new()
    {
        new EcosystemModule("Chat", "A porta de entrada humana", "💬", DesignSystem.AuraAccent, () => _chat),
        new EcosystemModule("Agente", "Orquestra decisões via LegalProcessEngine", "🧠", DesignSystem.AuraAccent2, () => _agent),
        new EcosystemModule("Células", "Autonomia isolada com propósito", "📊", DesignSystem.AuraAccent, () => _cells),
        new EcosystemModule("Runtime", "Executa comandos e programas", "⚡", DesignSystem.AuraAccent, () => _run),
        new EcosystemModule("Shell", "Comandos via /bin/sh no sandbox", "💻", DesignSystem.AuraAccent, () => _terminal),
        new EcosystemModule("Python", "Executor python3 (se disponível)", "🐍", DesignSystem.AuraAccent2, () => _executors),
        new EcosystemModule("Git", "Executor git (se disponível)", "📦", DesignSystem.AuraAccent2, () => _executors),
    };

    private async void OnModuleTapped(object sender, TappedEventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is not EcosystemModule module)
            return;

        try
        {
            await NavigateSafeAsync(module.Label, module.OpenPage());
        }
        catch (Exception ex)
        {
            AuraLog.Exception("EcosystemPage " + module.Label, ex);
        }
    }

    private async Task NavigateSafeAsync(string label, Page page)
    {
        for (int i = 0; i < Navigation.NavigationStack.Count; i++)
        {
            if (!ReferenceEquals(Navigation.NavigationStack[i], page))
                continue;
            while (Navigation.NavigationStack.Count > i + 1)
                await Navigation.PopAsync(false);
            return;
        }

        if (page.Parent != null)
        {
            await DisplayAlert(
                "AURA",
                "\"" + label + "\" já está aberta em outra aba. Feche-a lá ou use a barra de seções.",
                "OK");
            return;
        }

        await Navigation.PushAsync(page);
    }
}

public sealed class EcosystemModule
{
    public EcosystemModule(string label, string description, string icon, Color accent, Func<Page> openPage)
    {
        Label = label;
        Description = description;
        Icon = icon;
        Accent = accent;
        OpenPage = openPage;
    }

    public string Label { get; }
    public string Description { get; }
    public string Icon { get; }
    public Color Accent { get; }
    public Func<Page> OpenPage { get; }
}