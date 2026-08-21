#if ANDROID
using System;
using System.Text;
using Android.Content;
using Android.Hardware;
using Android.Media;
using Android.Provider;
using AndroidApplication = Android.App.Application;
using AndroidCameraManager = Android.Hardware.Camera2.CameraManager;
using AndroidProcess = Android.OS.Process;
using AndroidSecurityException = Java.Lang.SecurityException;

namespace AURA.Mobile.Platforms.Android
{
    /// <summary>
    /// Teste de acesso nativo às APIs Android.
    /// </summary>
    public static class AuraAndroidBridgeTest
    {
        public static string Run()
        {
            var r = new StringBuilder();
            var context = AndroidApplication.Context;

            r.AppendLine("=== AURA ANDROID NATIVE BRIDGE V16 ===");
            r.AppendLine($"UID={AndroidProcess.MyUid()}");
            r.AppendLine($"PACKAGE={context.PackageName}");

            Test(r, "PackageManager", () =>
                context.PackageManager?.GetPackageInfo(context.PackageName!, 0) != null);

            Test(r, "Settings.System READ", () =>
                Settings.System.GetString(context.ContentResolver, Settings.System.ScreenBrightness) != null);

            Test(r, "Settings.Secure READ", () =>
                !string.IsNullOrEmpty(Settings.Secure.GetString(context.ContentResolver, Settings.Secure.AndroidId)));

            Test(r, "SensorManager", () =>
            {
                var sm = context.GetSystemService(Context.SensorService) as SensorManager;
                return sm?.GetDefaultSensor(SensorType.Accelerometer) != null;
            });

            Test(r, "AudioManager", () =>
                context.GetSystemService(Context.AudioService) is AudioManager);

            Test(r, "CameraManager", () =>
                context.GetSystemService(Context.CameraService) is AndroidCameraManager);

            r.AppendLine("=== V16 DONE ===");
            return r.ToString();
        }

        private static void Test(StringBuilder r, string name, Func<bool> action)
        {
            try
            {
                r.AppendLine(action() ? $"[PASS] {name}" : $"[UNAVAILABLE] {name}");
            }
            catch (AndroidSecurityException ex)
            {
                r.AppendLine($"[DENIED] {name}: {ex.Message}");
            }
            catch (Exception ex)
            {
                r.AppendLine($"[ERROR] {name}: {ex.GetType().Name}: {ex.Message}");
            }
        }
    }
}
#endif
