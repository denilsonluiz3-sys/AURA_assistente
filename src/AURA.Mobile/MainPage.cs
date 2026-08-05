using AURA.Mobile.Pages;

namespace AURA.Mobile;

public class MainPage : TabbedPage
{
    public MainPage(
        HomePage home,
        ChatPage chat,
        MemoryPage memory,
        ExecutorsPage executors,
        ModulesPage modules,
        LogsPage logs,
        FixesPage fixes)
    {
        AuraLog.Info("MainPage.ctor BEGIN");
        Title = "AURA";
        BarBackgroundColor = Color.FromArgb("#101014");
        BarTextColor = Color.FromArgb("#f2f2f5");

        Children.Add(home);
        Children.Add(chat);
        Children.Add(memory);
        Children.Add(executors);
        Children.Add(modules);
        Children.Add(logs);
        Children.Add(fixes);
        AuraLog.Info("MainPage.ctor OK");
    }
}
