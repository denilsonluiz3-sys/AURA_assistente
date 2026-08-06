using AURA.Mobile.Pages;

namespace AURA.Mobile;

public class MainPage : TabbedPage
{
    public MainPage(
        HomePage home,
        ChatPage chat,
        AgentPage agent,
        MemoryPage memory,
        ExecutorsPage executors,
        ModulesPage modules,
        LogsPage logs,
        FixesPage fixes,
        TerminalPage terminal,
        BrowserPage browser,
        CellsPage cells,
        RunPage run)
    {
        AuraLog.Info("MainPage.ctor BEGIN");
        BarBackgroundColor = Color.FromArgb("#101014");
        BarTextColor = Color.FromArgb("#f2f2f5");

        Children.Add(MakeSection("Sistema",
            ("Início", home),
            ("Logs", logs),
            ("Correções", fixes)));
        Children.Add(MakeSection("Assistente",
            ("Chat", chat),
            ("Agente", agent),
            ("Memória", memory)));
        Children.Add(MakeSection("Ferramentas",
            ("Terminal", terminal),
            ("Executores", executors),
            ("Módulos", modules),
            ("Navegador", browser)));
        Children.Add(MakeSection("Apps",
            ("Células", cells),
            ("Rodar programa", run)));

        AuraLog.Info("MainPage.ctor OK");
    }

    private bool _permissionsAsked;

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_permissionsAsked)
            return;
        _permissionsAsked = true;

        try
        {
            await StoragePermissionHelper.EnsureStorageAccessAsync();

            if (!StoragePermissionHelper.IsAllFilesAccessGranted()
                && !Preferences.Get("all_files_access_asked", false))
            {
                Preferences.Set("all_files_access_asked", true);
                StoragePermissionHelper.RequestAllFilesAccess();
            }
        }
        catch (Exception ex)
        {
            AuraLog.Info("Permissões de armazenamento: " + ex.Message);
        }
    }

    private static NavigationPage MakeSection(string title, params (string Label, Page Page)[] items)
    {
        var section = new SectionPage(title, items);
        return new NavigationPage(section) { Title = title };
    }
}
