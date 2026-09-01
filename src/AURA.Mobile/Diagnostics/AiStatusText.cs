using AURA.AI.UniversalAI;

namespace AURA.Mobile.Diagnostics;

/// <summary>Linha de status curta para a UI do Agente (modelo · endpoint · key).</summary>
public static class AiStatusText
{
    public static string ForClient(IUniversalAiClient? client)
    {
        if (client == null)
            return "IA: não configurada";

        var model = string.IsNullOrWhiteSpace(client.Options.Model) ? "(sem modelo)" : client.Options.Model.Trim();
        var host = HostOf(client.Options.BaseUrl);
        var key = string.IsNullOrWhiteSpace(client.Options.ApiKey)
            && string.IsNullOrWhiteSpace(RuntimeConfig.ApiKey)
            ? "key: ausente"
            : "key: ok";

        if (string.IsNullOrWhiteSpace(host))
            return $"Modelo: {model} · {key}";

        return $"Modelo: {model} · {host} · {key}";
    }

    private static string HostOf(string? baseUrl)
    {
        var u = EndpointValidator.Normalize(baseUrl);
        if (string.IsNullOrEmpty(u))
            return string.Empty;
        try
        {
            return new Uri(u).Host;
        }
        catch
        {
            return u.Length > 40 ? u[..40] + "…" : u;
        }
    }
}
