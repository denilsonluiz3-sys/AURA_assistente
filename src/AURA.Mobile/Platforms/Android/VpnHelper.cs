using Android.Content;
using Android.Provider;
using AndroidApp = Android.App.Application;

namespace AURA.Mobile.Platforms.Android;

/// <summary>Abre configurações ou um provedor VPN externo; a AURA não implementa túnel nem armazena credenciais.</summary>
public static class VpnHelper
{
    public static void OpenExternalVpnSettings()
    {
        var intent = new Intent(Settings.ActionVpnSettings);
        intent.AddFlags(ActivityFlags.NewTask);
        AndroidApp.Context.StartActivity(intent);
    }

    public static bool IsExternalProviderInstalled(string packageName)
    {
        if (string.IsNullOrWhiteSpace(packageName)) return false;
        try { AndroidApp.Context.PackageManager.GetPackageInfo(packageName, 0); return true; }
        catch (Java.Lang.Exception) { return false; }
    }

    public static bool OpenExternalProvider(string packageName)
    {
        if (string.IsNullOrWhiteSpace(packageName)) return false;
        try
        {
            var launch = AndroidApp.Context.PackageManager.GetLaunchIntentForPackage(packageName);
            if (launch is null) return false;
            launch.AddFlags(ActivityFlags.NewTask);
            AndroidApp.Context.StartActivity(launch);
            return true;
        }
        catch (Java.Lang.Exception) { return false; }
    }
}
