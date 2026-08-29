using AURA.Core.Events;
using AURA.Mobile.Pages;
using AURA.Modules;

namespace AURA.Mobile
{
    public class MainPage : TabbedPage
    {
        private readonly ModuleManager _manager;
        private readonly List<(string? ModuleId, string Section, string Label, Page Page)> _entries;
        private bool _permissionsAsked;
        private CancellationTokenSource? _rebuildCts;

        public MainPage(EventBus events, ModuleManager manager, HomePage home, DiagnosticoPage diagnostico, ChatPage chat, AgentPage agent, MemoryPage memory, ExecutorsPage executors, ModulesPage modules, LogsPage logs, FixesPage fixes, TerminalPage terminal, BrowserPage browser, CellsPage cells, RunPage run, ProgramsPage programs, EcosystemPage ecosystem, SpectrumPage spectrum)
        {
            AuraLog.Info("MainPage.ctor BEGIN");
            _manager = manager;
            events.Subscribe<ModuleStateChangedEvent>(_ =>
                MainThread.BeginInvokeOnMainThread(ScheduleRebuildTabs));

            _entries = new List<(string?, string, string, Page)>
            {
                (null, "Sistema", "Início", home),
                (null, "Sistema", "Ecossistema", ecosystem),
                ("system", "Sistema", "Diagnóstico", diagnostico),
                ("logs", "Sistema", "Correções", fixes),

                ("ai", "Assistente", "Chat", chat),
                ("ai", "Assistente", "Agente", agent),
                ("memory", "Assistente", "Memória", memory),
                (null, "Assistente", "Navegador", browser),

                ("terminal", "Ferramentas", "Terminal", terminal),
                ("executors", "Ferramentas", "Executores", executors),
                (null, "Ferramentas", "Espectro", spectrum),
                (null, "Ferramentas", "Módulos", modules),
                ("logs", "Ferramentas", "Logs", logs),

                (null, "Apps", "Programas", programs),
                ("cells", "Apps", "Células", cells),
                ("cells", "Apps", "Rodar programa", run)
            };

            BarBackgroundColor = Color.FromArgb("#0c0c12");
            BarTextColor = Color.FromArgb("#e8e8f0");
            AuraLog.Info("MainPage.ctor OK");
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

        private void ScheduleRebuildTabs()
        {
            try { _rebuildCts?.Cancel(); } catch { /* ignore */ }
            _rebuildCts = new CancellationTokenSource();
            var token = _rebuildCts.Token;
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(250, token);
                    if (token.IsCancellationRequested) return;
                    MainThread.BeginInvokeOnMainThread(RebuildTabs);
                }
                catch (TaskCanceledException) { /* coalesced */ }
            });
        }

        public void RebuildTabs()
        {
            Children.Clear();
            foreach (IGrouping<string, (string ModuleId, string Section, string Label, Page Page)> group in _entries.GroupBy(e => e.Section))
            {
                var items = group.Where(e => e.ModuleId == null || _manager.IsApplied(e.ModuleId)).Select(e => (e.Label, e.Page)).ToArray();
                if (items.Length == 0) continue;
                Children.Add(MakeSection(group.Key, items));
            }
            AuraLog.Info("MainPage.RebuildTabs: " + Children.Count + " seções ativas");
        }

        public async Task NavigateToProcessAsync(string target)
        {
            var entry = _entries.FirstOrDefault(e => string.Equals(e.Label, target, StringComparison.OrdinalIgnoreCase));
            if (entry.Page == null) return;
            var section = Children.OfType<NavigationPage>().FirstOrDefault(n => string.Equals(n.Title, entry.Section, StringComparison.OrdinalIgnoreCase));
            if (section == null) return;
            CurrentPage = section;
            var navigationStack = section.Navigation.NavigationStack;
            for (int i = 0; i < navigationStack.Count; i++)
            {
                if (!ReferenceEquals(navigationStack[i], entry.Page)) continue;
                while (section.Navigation.NavigationStack.Count > i + 1) await section.PopAsync(false);
                return;
            }
            if (entry.Page.Parent == null) await section.PushAsync(entry.Page);
        }

        private static NavigationPage MakeSection(string title, params (string Label, Page Page)[] items)
        {
            var section = new SectionPage(title, items);
            return new NavigationPage(section) { Title = title };
        }
    }
}
