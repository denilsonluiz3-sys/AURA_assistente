#if ANDROID
using System;
using System.Text;
using Android.Content;
using Android.Hardware;
using Android.Hardware.Camera2;
using Android.Media;
using Android.Net;
using Android.OS;
using Android.Provider;

namespace AURA.Mobile.Platforms.Android;

/// <summary>
/// Safe, read-only probe of Android capabilities available to the AURA app UID.
/// No settings, files, device state, or external services are modified.
/// </summary>
public static class AuraAndroidBridgeTest
{
    /// <summary>
    /// Teste de acesso nativo às APIs Android.
    /// </summary>
    public static class AuraAndroidBridgeTest
    {
        var r = new StringBuilder();
        var context = Application.Context;

        r.AppendLine("=== AURA ANDROID CAPABILITY LAB V18 ===");
        r.AppendLine($"UID={Process.MyUid()}");
        r.AppendLine($"PACKAGE={context.PackageName}");
        r.AppendLine($"ANDROID={Build.VERSION.Release}");
        r.AppendLine($"SDK={Build.VERSION.SdkInt}");
        r.AppendLine($"ABI={Build.SupportedAbis?.Length > 0 ? Build.SupportedAbis[0] : "unknown"}");
        r.AppendLine();

        Test(r, "PackageManager", () =>
            context.PackageManager?.GetPackageInfo(context.PackageName!, 0) != null);

        Test(r, "Settings.System READ", () =>
            Settings.System.GetString(context.ContentResolver, Settings.System.ScreenBrightness) != null);

        Test(r, "Settings.Secure READ", () =>
            !string.IsNullOrEmpty(Settings.Secure.GetString(context.ContentResolver, Settings.Secure.AndroidId)));

        Test(r, "Accelerometer", () =>
            (context.GetSystemService(Context.SensorService) as SensorManager)
                ?.GetDefaultSensor(SensorType.Accelerometer) != null);

        Test(r, "Gyroscope", () =>
            (context.GetSystemService(Context.SensorService) as SensorManager)
                ?.GetDefaultSensor(SensorType.Gyroscope) != null);

        Test(r, "AudioManager", () =>
            context.GetSystemService(Context.AudioService) is AudioManager);

        Test(r, "CameraManager", () =>
            context.GetSystemService(Context.CameraService) is CameraManager);

        Test(r, "LocationManager", () =>
        {
            var r = new StringBuilder();
            var context = global::Android.App.Application.Context;

            r.AppendLine("=== AURA ANDROID NATIVE BRIDGE V16 ===");
            r.AppendLine($"UID={global::Android.OS.Process.MyUid()}");
            r.AppendLine($"PACKAGE={context.PackageName}");

            Test(r, "PackageManager", () =>
                context.PackageManager?.GetPackageInfo(
                    context.PackageName!, 0) != null);

            Test(r, "Settings.System READ", () =>
                Settings.System.GetString(
                    context.ContentResolver,
                    Settings.System.ScreenBrightness) != null);

            Test(r, "Settings.Secure READ", () =>
                !string.IsNullOrEmpty(
                    Settings.Secure.GetString(
                        context.ContentResolver,
                        Settings.Secure.AndroidId)));

            Test(r, "SensorManager", () =>
            {
                var sm = context.GetSystemService(Context.SensorService)
                         as SensorManager;
                return sm?.GetDefaultSensor(
                    SensorType.Accelerometer) != null;
            });

            Test(r, "AudioManager", () =>
                context.GetSystemService(Context.AudioService)
                is AudioManager);

            Test(r, "CameraManager", () =>
                context.GetSystemService(Context.CameraService)
                is global::Android.Hardware.Camera2.CameraManager);

            r.AppendLine("=== V16 DONE ===");
            return r.ToString();
        }

        private static void Test(
            StringBuilder r,
            string name,
            Func<bool> action)
        {
            try
            {
                r.AppendLine(action()
                    ? $"[PASS] {name}"
                    : $"[UNAVAILABLE] {name}");
            }
            catch (global::System.Security.SecurityException ex)
            {
                r.AppendLine($"[DENIED] {name}: {ex.Message}");
            }
            catch (Exception ex)
            {
                r.AppendLine(
                    $"[ERROR] {name}: {ex.GetType().Name}: {ex.Message}");
            }
        }
    }
}
#endif
