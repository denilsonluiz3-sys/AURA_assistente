using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AURA.AI;
using AURA.Mobile.Services;

namespace AURA.Mobile.Pages;

public sealed class AgentBrowserTool : AgentTool
{
    public override AgentToolDefinition Definition => new AgentToolDefinition
    {
        Name = "open_browser",
        Description = "Abre uma URL no navegador in-app da AURA (Navegador). Fallback: navegador externo.",
        Parameters =
        {
            ["url"] = new AgentToolParameter
            {
                Type = "string",
                Description = "URL completa a abrir (ex.: https://example.com)."
            }
        },
        Required = { "url" }
    };

    public override async Task<string> ExecuteAsync(string argumentsJson, CancellationToken ct = default)
    {
        string url;
        using (JsonDocument doc = JsonDocument.Parse(argumentsJson))
            url = ReadString(doc.RootElement, "url") ?? string.Empty;

        if (string.IsNullOrWhiteSpace(url))
            return "ERRO: URL vazia.";

        if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return "ERRO: URL deve começar com http:// ou https://";

        try
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await AuraBrowserBridge.OpenInAppBrowserAsync(url);
            });
            return "OK: navegador in-app com " + url;
        }
        catch (Exception ex)
        {
            try
            {
                await Microsoft.Maui.ApplicationModel.Browser.Default.OpenAsync(
                    new Uri(url), Microsoft.Maui.ApplicationModel.BrowserLaunchMode.External);
                return "OK: navegador externo com " + url + " (in-app falhou: " + ex.Message + ")";
            }
            catch (Exception ex2)
            {
                return "ERRO: não foi possível abrir o navegador: " + ex2.Message;
            }
        }
    }
}
