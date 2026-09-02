using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AURA.Abstractions;

namespace AURA.Agents.Programs;

internal abstract class BrowserActionCellProgram : IAuraCellProgram
{
    protected BrowserActionCellProgram(string name, string capability)
    {
        Name = name;
        RequiredCapabilities = new[] { capability };
    }

    public string Name { get; }
    public IReadOnlyCollection<string> RequiredCapabilities { get; }

    public abstract Task<CellProgramResult> ExecuteAsync(IAuraCellContext context, CancellationToken ct = default);

    protected static bool TryInt(IReadOnlyDictionary<string, string> args, string key, out int value)
        => args.TryGetValue(key, out var raw) && int.TryParse(raw, out value);
}

public sealed class BrowserReadCellProgram : BrowserActionCellProgram
{
    public BrowserReadCellProgram() : base("browser-read", "browser.read") { }

    public override async Task<CellProgramResult> ExecuteAsync(IAuraCellContext context, CancellationToken ct = default)
    {
        string? selector = context.Arguments.TryGetValue("selector", out var value) ? value : null;
        string text = await context.Browser.ReadAsync(selector, ct).ConfigureAwait(false);
        string domJson = await context.Browser.ReadDomAsync(selector, ct).ConfigureAwait(false);

        try
        {
            using var document = JsonDocument.Parse(domJson);
            return CellProgramResult.Ok(new
            {
                Selector = selector,
                Text = text,
                Dom = document.RootElement.Clone()
            });
        }
        catch (JsonException)
        {
            return CellProgramResult.Ok(new { Selector = selector, Text = text, Dom = new { Ok = false, Error = "DOM inválido retornado pelo navegador." } });
        }
    }
}

public sealed class BrowserClickCellProgram : BrowserActionCellProgram
{
    public BrowserClickCellProgram() : base("browser-click", "browser.click") { }

    public override async Task<CellProgramResult> ExecuteAsync(IAuraCellContext context, CancellationToken ct = default)
    {
        if (!context.Arguments.TryGetValue("selector", out var selector) || string.IsNullOrWhiteSpace(selector))
            return CellProgramResult.Fail("selector obrigatório.");
        bool ok = await context.Browser.ClickAsync(selector, ct).ConfigureAwait(false);
        return ok ? CellProgramResult.Ok(new { Selector = selector, Clicked = true }) : CellProgramResult.Fail("Elemento não encontrado ou clique falhou.");
    }
}

public sealed class BrowserTypeCellProgram : BrowserActionCellProgram
{
    public BrowserTypeCellProgram() : base("browser-type", "browser.type") { }

    public override async Task<CellProgramResult> ExecuteAsync(IAuraCellContext context, CancellationToken ct = default)
    {
        if (!context.Arguments.TryGetValue("selector", out var selector) || string.IsNullOrWhiteSpace(selector))
            return CellProgramResult.Fail("selector obrigatório.");
        if (!context.Arguments.TryGetValue("text", out var text))
            return CellProgramResult.Fail("text obrigatório.");
        bool ok = await context.Browser.TypeAsync(selector, text, ct).ConfigureAwait(false);
        return ok ? CellProgramResult.Ok(new { Selector = selector, Typed = true }) : CellProgramResult.Fail("Campo não encontrado ou preenchimento falhou.");
    }
}

public sealed class BrowserScrollCellProgram : BrowserActionCellProgram
{
    public BrowserScrollCellProgram() : base("browser-scroll", "browser.scroll") { }

    public override async Task<CellProgramResult> ExecuteAsync(IAuraCellContext context, CancellationToken ct = default)
    {
        if (!TryInt(context.Arguments, "pixels", out int pixels))
            return CellProgramResult.Fail("pixels obrigatório e deve ser inteiro.");
        bool ok = await context.Browser.ScrollAsync(Math.Clamp(pixels, -10000, 10000), ct).ConfigureAwait(false);
        return ok ? CellProgramResult.Ok(new { Pixels = pixels, Scrolled = true }) : CellProgramResult.Fail("Não foi possível rolar a página.");
    }
}

public sealed class BrowserBackCellProgram : BrowserActionCellProgram
{
    public BrowserBackCellProgram() : base("browser-back", "browser.back") { }

    public override async Task<CellProgramResult> ExecuteAsync(IAuraCellContext context, CancellationToken ct = default)
    {
        bool ok = await context.Browser.BackAsync(ct).ConfigureAwait(false);
        return ok ? CellProgramResult.Ok(new { Navigated = true }) : CellProgramResult.Fail("Não há histórico anterior.");
    }
}

public sealed class BrowserForwardCellProgram : BrowserActionCellProgram
{
    public BrowserForwardCellProgram() : base("browser-forward", "browser.forward") { }

    public override async Task<CellProgramResult> ExecuteAsync(IAuraCellContext context, CancellationToken ct = default)
    {
        bool ok = await context.Browser.ForwardAsync(ct).ConfigureAwait(false);
        return ok ? CellProgramResult.Ok(new { Navigated = true }) : CellProgramResult.Fail("Não há histórico seguinte.");
    }
}

public sealed class BrowserWaitCellProgram : BrowserActionCellProgram
{
    public BrowserWaitCellProgram() : base("browser-wait", "browser.wait") { }

    public override async Task<CellProgramResult> ExecuteAsync(IAuraCellContext context, CancellationToken ct = default)
    {
        if (!TryInt(context.Arguments, "milliseconds", out int milliseconds))
            return CellProgramResult.Fail("milliseconds obrigatório e deve ser inteiro.");
        bool ok = await context.Browser.WaitAsync(Math.Clamp(milliseconds, 0, 30000), ct).ConfigureAwait(false);
        return ok ? CellProgramResult.Ok(new { Milliseconds = milliseconds, Waited = true }) : CellProgramResult.Fail("Tempo de espera inválido.");
    }
}

public sealed class BrowserScreenshotCellProgram : BrowserActionCellProgram
{
    public BrowserScreenshotCellProgram() : base("browser-screenshot", "browser.screenshot") { }

    public override async Task<CellProgramResult> ExecuteAsync(IAuraCellContext context, CancellationToken ct = default)
    {
        string? path = await context.Browser.ScreenshotAsync(ct).ConfigureAwait(false);
        return path != null
            ? CellProgramResult.Ok(new { Path = path, Captured = true })
            : CellProgramResult.Fail("Não foi possível capturar o navegador.");
    }
}
