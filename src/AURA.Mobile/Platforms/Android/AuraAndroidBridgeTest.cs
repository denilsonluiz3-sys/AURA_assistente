#if ANDROID
using System;
using System.Text;
using Android.App;
using Android.Content;
using Android.Hardware;
using Android.Media;
using Android.Provider;

namespace AURA.Mobile.Platforms.Android
{
    /// <summary>
    /// V16 - Teste de acesso nativo às APIs Android
    /// Descobre se a AURA consegue usar diretamente as APIs nativas
    /// </summary>
    public static class AuraAndroidBridgeTest
    {
        public static string Run()
        {
            var r = new StringBuilder();
            var context = Application.Context;

            r.AppendLine("=== AURA ANDROID NATIVE BRIDGE V16 ===");
            r.AppendLine($"UID={Android.OS.Process.MyUid()}");
            r.AppendLine($"PACKAGE={context.PackageName}");

            // 1. PackageManager - acesso nativo ao próprio pacote
            Test(r, "PackageManager", () =>
                context.PackageManager?.GetPackageInfo(
                    context.PackageName!, 0) != null);

            // 2. Settings.System - leitura via ContentResolver
            Test(r, "Settings.System READ", () =>
                Settings.System.GetString(
                    context.ContentResolver,
                    Settings.System.ScreenBrightness) != null);

            // 3. Settings.Secure - leitura de informação protegida
            Test(r, "Settings.Secure READ", () =>
                !string.IsNullOrEmpty(
                    Settings.Secure.GetString(
                        context.ContentResolver,
                        Settings.Secure.AndroidId)));

            // 4. SensorManager - acesso real à API de sensores
            Test(r, "SensorManager", () =>
            {
                var sm = context.GetSystemService(Context.SensorService)
                         as SensorManager;
                return sm?.GetDefaultSensor(
                    SensorType.Accelerometer) != null;
            });

            // 5. AudioManager - acesso à API de áudio
            Test(r, "AudioManager", () =>
                context.GetSystemService(Context.AudioService)
                is AudioManager);

            // 6. CameraManager - acesso à API de câmera
            Test(r, "CameraManager", () =>
                context.GetSystemService(Context.CameraService)
                is CameraManager);

            r.AppendLine("=== V16 DONE ===");
            return r.ToString();
        }

        static void Test(
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
            catch (SecurityException ex)
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
