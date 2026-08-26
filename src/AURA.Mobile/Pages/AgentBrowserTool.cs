using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AURA.AI;

namespace AURA.Mobile.Pages;

public sealed class AgentBrowserTool : AgentTool
{
    public override AgentToolDefinition Definition => new AgentToolDefinition
    {
        Name = "open_browser",
        Description = "Abre uma URL no navegador padrão do dispositivo.",
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

    public override Task<string> ExecuteAsync(string argumentsJson, CancellationToken ct = default)
    {
        string url;
        using (JsonDocument doc = JsonDocument.Parse(argumentsJson))
            url = ReadString(doc.RootElement, "url") ?? string.Empty;

        if (string.IsNullOrWhiteSpace(url))
            return Task.FromResult("ERRO: URL vazia.");

        if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult("ERRO: URL deve começar com http:// ou https://");

        try
        {
            _ = Microsoft.Maui.ApplicationModel.Browser.Default.OpenAsync(
                new Uri(url), Microsoft.Maui.ApplicationModel.BrowserLaunchMode.External);
            return Task.FromResult("OK: navegador aberto com " + url);
        }
        catch (Exception ex)
        {
            return Task.FromResult("ERRO: não foi possível abrir o navegador: " + ex.Message);
        }
    }
}
