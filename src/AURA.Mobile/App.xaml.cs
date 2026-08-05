namespace AURA.Mobile;

public partial class App : Application
{
    public App(MainPage mainPage)
    {
        AuraLog.Info("App.ctor BEGIN");
        try
        {
            InitializeComponent();
            AuraLog.Info("App.ctor InitializeComponent OK");
            MainPage = mainPage;
            AuraLog.Info("App.ctor MainPage set OK");
        }
        catch (Exception ex)
        {
            AuraLog.Exception("App.ctor", ex);
            throw;
        }
    }

    protected override void OnStart()
    {
        base.OnStart();
        AuraLog.Info("App.OnStart");
    }

    protected override void OnSleep()
    {
        AuraLog.Info("App.OnSleep");
        base.OnSleep();
    }

    protected override void OnResume()
    {
        base.OnResume();
        AuraLog.Info("App.OnResume");
    }
}
