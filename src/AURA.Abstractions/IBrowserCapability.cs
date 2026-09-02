using System.Threading;
using System.Threading.Tasks;

namespace AURA.Abstractions;

/// <summary>
/// Capacidade de automação do navegador da AURA.
/// Mantém a API independente da implementação Android/WebView.
/// </summary>
public interface IBrowserCapability
{
    bool IsAvailable { get; }
    Task<bool> OpenAsync(string url, CancellationToken ct = default);
    Task<string> ReadAsync(string? selector = null, CancellationToken ct = default);
    Task<string> ReadDomAsync(string? selector = null, CancellationToken ct = default);
    Task<bool> ClickAsync(string selector, CancellationToken ct = default);
    Task<bool> TypeAsync(string selector, string text, CancellationToken ct = default);
    Task<bool> ScrollAsync(int pixels, CancellationToken ct = default);
    Task<bool> BackAsync(CancellationToken ct = default);
    Task<bool> ForwardAsync(CancellationToken ct = default);
    Task<bool> WaitAsync(int milliseconds, CancellationToken ct = default);
    Task<string?> ScreenshotAsync(CancellationToken ct = default);
}
