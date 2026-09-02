using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AURA.Abstractions;

namespace AURA.Agents.Programs;

/// <summary>
/// Cell Program que usa a capacidade real de navegador do dispositivo.
/// A URL é fornecida nos argumentos do contexto para permitir reutilização sem LLM.
/// </summary>
public sealed class BrowserCellProgram : IAuraCellProgram
{
    public string Name => "browser-open";

    public IReadOnlyCollection<string> RequiredCapabilities { get; } = new[]
    {
        "browser.open"
    };

    public async Task<CellProgramResult> ExecuteAsync(IAuraCellContext context, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (!context.Arguments.TryGetValue("url", out string? url) || string.IsNullOrWhiteSpace(url))
            return CellProgramResult.Fail("URL obrigatória. Use arguments.url.");

        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            return CellProgramResult.Fail("URL inválida. Apenas http:// e https:// são permitidos.");

        if (!context.Browser.IsAvailable)
            return CellProgramResult.Fail("Navegador não disponível neste dispositivo.");

        bool opened = await context.Browser.OpenAsync(uri.ToString(), ct).ConfigureAwait(false);
        return opened
            ? CellProgramResult.Ok(new { Url = uri.ToString(), Opened = true })
            : CellProgramResult.Fail("Não foi possível abrir a URL no navegador.");
    }
}
