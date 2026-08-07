using Android.Content;

namespace AURA.Mobile.Platforms.Android
{
    /// <summary>
    /// Integração com VPN/Tor no Android. Não embute túnel (seria um SDK
    /// nativo inviável aqui); abre as configurações de VPN do sistema e o Orbot
    /// (para .onion), e oferece a instalação quando ausente.
    /// </summary>
    public static class VpnHelper
    {
        private const string OrbotPackage = "org.torproject.android";

        public static void OpenVpnSettings()
        {
            var intent = new Intent(Android.Provider.Settings.ActionVpnSettings);
            intent.AddFlags(ActivityFlags.NewTask);
            Android.App.Application.Context.StartActivity(intent);
        }

        public static bool IsOrbotInstalled()
        {
            try
            {
                Android.App.Application.Context.PackageManager.GetPackageInfo(OrbotPackage, 0);
                return true;
            }
            catch (Java.Lang.Exception)
            {
                return false;
            }
        }

        public static bool OpenOrbot()
        {
            try
            {
                Intent launch = Android.App.Application.Context
                    .PackageManager.GetLaunchIntentForPackage(OrbotPackage);
                if (launch == null)
                {
                    return false;
                }

                launch.AddFlags(ActivityFlags.NewTask);
                Android.App.Application.Context.StartActivity(launch);
                return true;
            }
            catch (Java.Lang.Exception)
            {
                return false;
            }
        }

        public const string OrbotPlayStoreUrl =
            "https://play.google.com/store/apps/details?id=org.torproject.android";
    }
}
