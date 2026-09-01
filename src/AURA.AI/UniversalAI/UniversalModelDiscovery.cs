using System.Text.Json;

namespace AURA.AI.UniversalAI;

public sealed class UniversalModelDiscovery
{
    private readonly HttpClient _http;
    public UniversalModelDiscovery(HttpClient http) => _http = http ?? throw new ArgumentNullException(nameof(http));

    public async Task<IReadOnlyList<UniversalModel>> LoadAsync(UniversalProvider provider, string apiKey, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, provider.ModelsUrl);
        if (!string.IsNullOrWhiteSpace(apiKey))
            request.Headers.TryAddWithoutValidation(provider.AuthHeader, string.IsNullOrWhiteSpace(provider.AuthScheme) ? apiKey.Trim() : provider.AuthScheme + " " + apiKey.Trim());
        using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Falha ao carregar modelos de {provider.Name}: {(int)response.StatusCode} {response.ReasonPhrase}. {body[..Math.Min(body.Length, 500)]}");
        return Parse(provider.Id, body);
    }

    private static IReadOnlyList<UniversalModel> Parse(string providerId, string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var result = new List<UniversalModel>();
        if (root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in data.EnumerateArray())
            {
                if (!item.TryGetProperty("id", out var id)) continue;
                var value = id.GetString();
                if (!string.IsNullOrWhiteSpace(value)) result.Add(new UniversalModel(value!, value!, providerId, item.TryGetProperty("owned_by", out var o) ? o.GetString() : null));
            }
        }
        else if (root.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in root.EnumerateArray())
            {
                var value = item.ValueKind == JsonValueKind.String ? item.GetString() : item.TryGetProperty("name", out var n) ? n.GetString() : null;
                if (!string.IsNullOrWhiteSpace(value)) result.Add(new UniversalModel(value!, value!, providerId));
            }
        }
        return result.OrderBy(x => x.DisplayName, StringComparer.OrdinalIgnoreCase).ToArray();
    }
}
