using AURA.Mobile.Pages;

namespace AURA.Mobile
{
    public class MainPage : TabbedPage
    {
        private readonly List<(string Section, string Label, Page Page)> _entries;
        private bool _permissionsAsked;
        private Page? _advancedMenu;

        private static readonly HashSet<string> PrimaryTabs = new(StringComparer.OrdinalIgnoreCase)
        {
            "Agente", "Navegador"
        };

        public MainPage(
            AgentPage agent,
            TerminalPage terminal,
            BrowserPage browser,
            CellsPage cells)
        {
            AuraLog.Info("MainPage.ctor BEGIN");
            // Navegação principal: Agente como início; recursos secundários em Mais.
            // As antigas categorias Sistema/Assistente/Ferramentas/Apps duplicavam
            // os mesmos atalhos em várias telas.
            var advancedItems = new (string Label, Page Page)[]
            {
                ("Terminal", terminal),
                ("Células", cells),
            };
            var advancedMenu = new SectionPage("Modo avançado", advancedItems);
            _advancedMenu = advancedMenu;

            _entries = new List<(string Section, string Label, Page Page)>
            {
                // O Agente é a tela inicial e o único destino primário.
                ("Agente", "Agente", agent),

                // Recursos que ainda acrescentam uma função própria.
                ("Navegador", "Navegador", browser),
                ("Mais", "Modo avançado", advancedMenu),

                // Ferramentas técnicas continuam disponíveis em Modo avançado.
                ("Avançado", "Terminal", terminal),
                ("Avançado", "Células", cells),
            };

            BarBackgroundColor = Color.FromArgb("#0c0c12");
            BarTextColor = Color.FromArgb("#e8e8f0");
            SelectedTabColor = Color.FromArgb("#7a9eff");
            UnselectedTabColor = Color.FromArgb("#7a7f94");
#if ANDROID
            Microsoft.Maui.Controls.PlatformConfiguration.AndroidSpecific.TabbedPage.SetToolbarPlacement(
                On<Microsoft.Maui.Controls.PlatformConfiguration.Android>(),
                Microsoft.Maui.Controls.PlatformConfiguration.AndroidSpecific.ToolbarPlacement.Bottom);
#endif
            AuraLog.Info("MainPage.ctor OK (navegação principal consolidada)");
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            RebuildTabs();
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

        public void RebuildTabs()
        {
            Children.Clear();

            foreach (var entry in _entries.Where(e => PrimaryTabs.Contains(e.Section)))
            {
                Children.Add(new NavigationPage(entry.Page) { Title = entry.Section });
            }

            var moreItems = _entries
                .Where(e => e.Section.Equals("Mais", StringComparison.OrdinalIgnoreCase))
                .Select(e => (e.Label, e.Page))
                .ToArray();
            if (moreItems.Length > 0)
                Children.Add(new NavigationPage(new SectionPage("Mais", moreItems)) { Title = "Mais" });

            // O retorno à raiz deve sempre abrir o Agente, não a antiga tela Sistema.
            if (Children.Count > 0)
                CurrentPage = Children[0];
            AuraLog.Info("MainPage.RebuildTabs: " + Children.Count + " destinos principais (Agente como início)");
        }

        public async Task NavigateToProcessAsync(string target)
        {
            // Rota legada "Chat" → Agente (um único ponto inteligente).
            if (string.Equals(target, "Chat", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(target, "Assistente", StringComparison.OrdinalIgnoreCase))
                target = "Agente";

            var entry = _entries.FirstOrDefault(e =>
                string.Equals(e.Label, target, StringComparison.OrdinalIgnoreCase));
            if (entry.Page == null) return;

            if (PrimaryTabs.Contains(entry.Section))
            {
                var direct = Children.OfType<NavigationPage>()
                    .FirstOrDefault(n => string.Equals(n.Title, entry.Section, StringComparison.OrdinalIgnoreCase));
                if (direct != null)
                    CurrentPage = direct;
                return;
            }

            if (entry.Section.Equals("Avançado", StringComparison.OrdinalIgnoreCase) && _advancedMenu != null)
            {
                var moreForAdvanced = Children.OfType<NavigationPage>()
                    .FirstOrDefault(n => string.Equals(n.Title, "Mais", StringComparison.OrdinalIgnoreCase));
                if (moreForAdvanced == null) return;
                CurrentPage = moreForAdvanced;

                if (!moreForAdvanced.Navigation.NavigationStack.Contains(_advancedMenu))
                    await moreForAdvanced.PushAsync(_advancedMenu, false);
                if (entry.Page.Parent == null)
                    await moreForAdvanced.PushAsync(entry.Page);
                return;
            }

            var more = Children.OfType<NavigationPage>()
                .FirstOrDefault(n => string.Equals(n.Title, "Mais", StringComparison.OrdinalIgnoreCase));
            if (more == null) return;
            CurrentPage = more;

            for (int i = 0; i < more.Navigation.NavigationStack.Count; i++)
            {
                if (!ReferenceEquals(more.Navigation.NavigationStack[i], entry.Page)) continue;
                while (more.Navigation.NavigationStack.Count > i + 1)
                    await more.PopAsync(false);
                return;
            }

            if (entry.Page.Parent == null)
                await more.PushAsync(entry.Page);
        }
    }
}
