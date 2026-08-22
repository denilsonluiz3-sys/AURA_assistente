#if ANDROID
using System;
using System.Text;
using Android.App;
using Android.Bluetooth;
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
    public static string Run()
    {
        var r = new StringBuilder();
        var context = Application.Context;

        r.AppendLine("=== AURA ANDROID CAPABILITY LAB V18 ===");
        r.AppendLine($"UID={Process.MyUid()}");
        r.AppendLine($"PACKAGE={context.PackageName}");
        r.AppendLine($"ANDROID={Build.VERSION.Release}");
        r.AppendLine($"SDK={Build.VERSION.SdkInt}");
        r.AppendLine($"ABI={(Build.SupportedAbis?.Length > 0 ? Build.SupportedAbis[0] : "unknown")}");
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
            var manager = context.GetSystemService(Context.LocationService) as LocationManager;
            return manager?.GetProviders(true)?.Count > 0;
        });

        Test(r, "ConnectivityManager", () =>
        {
            var manager = context.GetSystemService(Context.ConnectivityService) as ConnectivityManager;
            var network = manager?.ActiveNetwork;
            return network != null && manager?.GetNetworkCapabilities(network) != null;
        });

        Test(r, "BatteryManager", () =>
            context.GetSystemService(Context.BatteryService) is BatteryManager);

        Test(r, "BluetoothManager", () =>
            context.GetSystemService(Context.BluetoothService) is BluetoothManager);

        Test(r, "VibratorManager", () =>
            Build.VERSION.SdkInt >= BuildVersionCodes.S
                && context.GetSystemService(Context.VibratorManagerService) is VibratorManager);

        Test(r, "NotificationManager", () =>
            context.GetSystemService(Context.NotificationService) is NotificationManager);

        r.AppendLine();
        r.AppendLine("=== END V18 ===");
        return r.ToString();
    }

    private static void Test(StringBuilder r, string name, Func<bool> action)
    {
        try
        {
            r.AppendLine(action() ? $"[PASS] {name}" : $"[UNAVAILABLE] {name}");
        }
        catch (Exception ex)
        {
            r.AppendLine($"[BLOCKED] {name}: {ex.GetType().Name}: {ex.Message}");
        }
    }
}
#endif
