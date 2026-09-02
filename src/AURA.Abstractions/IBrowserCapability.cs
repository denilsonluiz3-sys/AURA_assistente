using System.Threading;
using System.Threading.Tasks;

namespace AURA.Abstractions;

/// <summary>
/// Capacidade neutra para abrir conteúdo web no dispositivo.
/// Não expõe APIs específicas de plataforma ao núcleo de Cell Programs.
/// </summary>
public interface IBrowserCapability
{
    bool IsAvailable { get; }
    Task<bool> OpenAsync(string url, CancellationToken ct = default);
}
