using Android.App;
using Android.Content.PM;
using Android.OS;

namespace AURA.Mobile;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode |
        ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        AuraLog.Info("MainActivity.OnCreate BEGIN");
        try
        {
            base.OnCreate(savedInstanceState);
            AuraLog.Info("MainActivity.OnCreate OK");
        }
        catch (Exception ex)
        {
            AuraLog.Exception("MainActivity.OnCreate", ex);
            throw;
        }
    }

    protected override void OnResume()
    {
        base.OnResume();
        AuraLog.Info("MainActivity.OnResume OK");
    }

    protected override void OnDestroy()
    {
        AuraLog.Info("MainActivity.OnDestroy");
        base.OnDestroy();
    }
}
