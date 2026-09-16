using AURA.Mobile.Pages;

namespace AURA.Mobile;

/// <summary>
/// Navegação principal com o Agente como início e uma aba dedicada ao Navegador.
/// </summary>
public class MainPage : TabbedPage
{
    private readonly NavigationPage _agentTab;
    private readonly NavigationPage _browserTab;
    private readonly List<(string Section, string Label, Page Page)> _entries;
    private readonly Page _advancedMenu;
    private bool _permissionsAsked;

    public MainPage(AgentPage agent, TerminalPage terminal, BrowserPage browser, CellsPage cells)
    {
        AuraLog.Info("MainPage.ctor BEGIN");
        _agentTab = CreateTab(agent, "Agente", "⌂");
        _browserTab = CreateTab(browser, "Navegador", "◉");
        Children.Add(_agentTab);
        Children.Add(_browserTab);
        CurrentPage = _agentTab;

        _advancedMenu = new SectionPage("Modo avançado", new (string Label, Page Page)[]
        {
            ("Terminal", terminal),
            ("Células", cells),
        });
        _entries = new List<(string Section, string Label, Page Page)>
        {
            ("Agente", "Agente", agent),
            ("Navegador", "Navegador", browser),
            ("Avançado", "Terminal", terminal),
            ("Avançado", "Células", cells),
        };

        BarBackgroundColor = Color.FromArgb("#0d0f18");
        BarTextColor = Color.FromArgb("#eef0f5");
        HideNativeTitleBar();
        AuraLog.Info("MainPage.ctor OK (abas Agente/Navegador)");
    }

    private static void HideNativeTitleBar()
    {
#if ANDROID
        try
        {
            Microsoft.Maui.ApplicationModel.Platform.CurrentActivity?.ActionBar?.Hide();
        }
        catch (Exception ex)
        {
            AuraLog.Exception("MainPage.HideNativeTitleBar", ex);
        }
#endif
    }

    private static NavigationPage CreateTab(Page page, string title, string icon)
    {
        // O título grande "Agente" vinha da barra de navegação da NavigationPage
        // interna. A aba mantém o título, mas a barra visual fica totalmente oculta.
        NavigationPage.SetHasNavigationBar(page, false);
        var navigation = new NavigationPage(page)
        {
            Title = title,
            IconImageSource = null
        };
        NavigationPage.SetHasNavigationBar(navigation, false);
        return navigation;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_permissionsAsked) return;
        _permissionsAsked = true;
        try
        {
            await StoragePermissionHelper.EnsureStorageAccessAsync();
            if (!StoragePermissionHelper.IsAllFilesAccessGranted() && !Preferences.Get("all_files_access_asked", false))
            {
                Preferences.Set("all_files_access_asked", true);
                StoragePermissionHelper.RequestAllFilesAccess();
            }
        }
        catch (Exception ex) { AuraLog.Info("Permissões de armazenamento: " + ex.Message); }
    }

    public void RebuildTabs() => AuraLog.Info("MainPage.RebuildTabs: abas Agente/Navegador já estão montadas");

    public async Task NavigateToProcessAsync(string target)
    {
        if (string.Equals(target, "Chat", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(target, "Assistente", StringComparison.OrdinalIgnoreCase)) target = "Agente";

        var entry = _entries.FirstOrDefault(e => string.Equals(e.Label, target, StringComparison.OrdinalIgnoreCase));
        if (entry.Page == null) return;

        if (entry.Section.Equals("Agente", StringComparison.OrdinalIgnoreCase))
        {
            while (_agentTab.Navigation.NavigationStack.Count > 1) await _agentTab.PopAsync(false);
            CurrentPage = _agentTab;
            return;
        }

        if (entry.Section.Equals("Navegador", StringComparison.OrdinalIgnoreCase))
        {
            CurrentPage = _browserTab;
            return;
        }

        if (entry.Section.Equals("Avançado", StringComparison.OrdinalIgnoreCase))
        {
            CurrentPage = _agentTab;
            if (!_agentTab.Navigation.NavigationStack.Contains(_advancedMenu))
                await _agentTab.PushAsync(_advancedMenu, false);
            if (!_agentTab.Navigation.NavigationStack.Contains(entry.Page))
                await _agentTab.PushAsync(entry.Page);
            else
            {
                while (!ReferenceEquals(_agentTab.CurrentPage, entry.Page)) await _agentTab.PopAsync(false);
            }
        }
    }
}
