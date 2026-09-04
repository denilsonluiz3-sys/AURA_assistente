namespace AURA.Mobile.Pages;

/// <summary>
/// Ecossistema AURA: hub visual com navegação direta para os módulos reais.
/// </summary>
public partial class EcosystemPage : ContentPage
{
    private readonly ChatPage _chat;
    private readonly AgentPage _agent;
    private readonly MemoryPage _memory;
    private readonly CellsPage _cells;
    private readonly RunPage _run;
    private readonly ProgramsPage _programs;
    private readonly TerminalPage _terminal;
    private readonly ExecutorsPage _executors;
    private readonly WorkspacePage _workspace;
    private readonly LogsPage _logs;
    private readonly FixesPage _fixes;
    private readonly SpectrumPage _spectrum;
    private readonly BrowserPage _browser;
    private readonly ModulesPage _modules;

    public EcosystemPage(
        ChatPage chat,
        AgentPage agent,
        MemoryPage memory,
        CellsPage cells,
        RunPage run,
        ProgramsPage programs,
        TerminalPage terminal,
        ExecutorsPage executors,
        WorkspacePage workspace,
        LogsPage logs,
        FixesPage fixes,
        SpectrumPage spectrum,
        BrowserPage browser,
        ModulesPage modules)
    {
        InitializeComponent();
        _chat = chat;
        _agent = agent;
        _memory = memory;
        _cells = cells;
        _run = run;
        _programs = programs;
        _terminal = terminal;
        _executors = executors;
        _workspace = workspace;
        _logs = logs;
        _fixes = fixes;
        _spectrum = spectrum;
        _browser = browser;
        _modules = modules;
        BindableLayout.SetItemsSource(ModulesHost, BuildModules());
    }

    private List<EcosystemModule> BuildModules() => new()
    {
        new EcosystemModule("Chat", "Conversa direta com a AURA", "💬", DesignSystem.AuraAccent, () => _chat),
        new EcosystemModule("Agente", "Loop com tools e workspace", "🧠", DesignSystem.AuraAccent2, () => _agent),
        new EcosystemModule("Memória", "Histórico e memórias persistentes", "📒", DesignSystem.AuraAccent, () => _memory),
        new EcosystemModule("Workspace", "Word, PDF e pedido ao agente", "📄", DesignSystem.AuraAccent, () => _workspace),
        new EcosystemModule("Navegador", "WebView e automação de páginas", "🌐", DesignSystem.AuraAccent2, () => _browser),
        new EcosystemModule("Células", "Programas Cell isolados", "📊", DesignSystem.AuraAccent, () => _cells),
        new EcosystemModule("Programas", "Lista e executa programas", "▣", DesignSystem.AuraAccent, () => _programs),
        new EcosystemModule("Rodar programa", "Runtime de execução", "⚡", DesignSystem.AuraAccent, () => _run),
        new EcosystemModule("Terminal", "Shell no sandbox", "💻", DesignSystem.AuraAccent, () => _terminal),
        new EcosystemModule("Executores", "Python, Git e Node", "▶", DesignSystem.AuraAccent2, () => _executors),
        new EcosystemModule("Módulos", "Ativar e gerenciar módulos", "⊞", DesignSystem.AuraAccent, () => _modules),
        new EcosystemModule("Logs", "Registros do sistema", "≡", DesignSystem.AuraAccent, () => _logs),
        new EcosystemModule("Correções", "Sugestões e fixes", "⚕", DesignSystem.AuraAccent2, () => _fixes),
        new EcosystemModule("Espectro", "Sensores e espectro", "〰", DesignSystem.AuraAccent, () => _spectrum),
    };

    private async void OnModuleTapped(object sender, TappedEventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is not EcosystemModule module) return;
        try { await NavigateSafeAsync(module.Label, module.OpenPage()); }
        catch (Exception ex) { AuraLog.Exception("EcosystemPage " + module.Label, ex); }
    }

    private async Task NavigateSafeAsync(string label, Page page)
    {
        for (int i = 0; i < Navigation.NavigationStack.Count; i++)
        {
            if (!ReferenceEquals(Navigation.NavigationStack[i], page)) continue;
            while (Navigation.NavigationStack.Count > i + 1) await Navigation.PopAsync(false);
            return;
        }
        if (page.Parent != null)
        {
            await DisplayAlert("AURA", "\"" + label + "\" já está aberta em outra aba. Feche-a lá ou use a barra de seções.", "OK");
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
