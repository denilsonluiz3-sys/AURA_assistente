namespace AURA.Abstractions;

/// <summary>
/// Capacidade neutra para diagnóstico do dispositivo.
/// Não expõe APIs Android nem tipos específicos de plataforma.
/// </summary>
public interface IDeviceDiagnosticCapability
{
    string GetDevice();
    string GetProperties();
    string GetBattery();
    string GetNetwork();
}
