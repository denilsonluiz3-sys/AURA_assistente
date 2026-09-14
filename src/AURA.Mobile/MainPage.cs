using AURA.Mobile.Pages;

namespace AURA.Mobile
{
    // Raiz de navegação sem abas inferiores: o Agente permanece o início e as
    // páginas secundárias continuam acessíveis por NavigateToProcessAsync.
    public class MainPage : NavigationPage
    {
        private readonly List<(string Section, string Label, Page Page)> _entries;
        private readonly Page _advancedMenu;
        private bool _permissionsAsked;

        public MainPage(AgentPage agent, TerminalPage terminal, BrowserPage browser, CellsPage cells)
            : base(agent)
        {
            AuraLog.Info("MainPage.ctor BEGIN");
            NavigationPage.SetHasNavigationBar(this, false);
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
            AuraLog.Info("MainPage.ctor OK (raiz sem barra inferior)");
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

        // Mantido para compatibilidade com chamadas existentes; não há abas para reconstruir.
        public void RebuildTabs() => AuraLog.Info("MainPage.RebuildTabs: navegação em pilha (sem atalhos inferiores)");

        public async Task NavigateToProcessAsync(string target)
        {
            if (string.Equals(target, "Chat", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(target, "Assistente", StringComparison.OrdinalIgnoreCase))
                target = "Agente";

            var entry = _entries.FirstOrDefault(e => string.Equals(e.Label, target, StringComparison.OrdinalIgnoreCase));
            if (entry.Page == null) return;
            if (ReferenceEquals(entry.Page, CurrentPage)) return;

            if (entry.Section.Equals("Agente", StringComparison.OrdinalIgnoreCase))
            {
                while (Navigation.NavigationStack.Count > 1)
                    await Navigation.PopAsync(false);
                return;
            }

            if (entry.Section.Equals("Avançado", StringComparison.OrdinalIgnoreCase))
            {
                if (!Navigation.NavigationStack.Contains(_advancedMenu))
                    await PushAsync(_advancedMenu, false);
                if (!Navigation.NavigationStack.Contains(entry.Page))
                    await PushAsync(entry.Page);
                else
                {
                    while (Navigation.NavigationStack.Count > Navigation.NavigationStack.IndexOf(entry.Page) + 1)
                        await Navigation.PopAsync(false);
                }
                return;
            }

            if (Navigation.NavigationStack.Contains(entry.Page))
            {
                while (!ReferenceEquals(CurrentPage, entry.Page))
                    await Navigation.PopAsync(false);
                return;
            }
            await PushAsync(entry.Page);
        }
    }
}
