namespace AURA.Abstractions;

/// <summary>
/// Contrato neutro para capacidades nativas Android.
/// A camada de abstrações não referencia APIs Android.
/// </summary>
public interface IAndroidCapabilityService
{
    string GetBattery();
    string GetLight();
    string GetAccelerometer();
    string GetGyroscope();
    string GetMagnetometer();

    /// <summary>
    /// Amostra o campo magnético por alguns ms e resume magnitude + Hz estimado.
    /// </summary>
    string SampleMagnetometer(int durationMs = 400);

    string GetLocation();
    string GetCameras();
    string GetAudio();
    string GetBluetooth();
    string GetClipboard();
    string SetClipboard(string text);
    string Notify(string title, string body);
    string Vibrate(int ms);
    string GetNetwork();
    string GetDevice();
    string GetApps();
    string GetAppCatalog();
    string FindApp(string query);
    string LaunchApp(string packageName);
    string GetProperties();
    string GetMemory();
    string GetStorage();
    string GetAll();
}
