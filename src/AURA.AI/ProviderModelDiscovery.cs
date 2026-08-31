using System.Net.Http.Headers;
using System.Text.Json;

namespace AURA.AI
{
    public sealed class DiscoveredModel
    {
        public string Id { get; init; } = string.Empty;
        public string Label => Id;
        public string? OwnedBy { get; init; }
    }

    /// <summary>
    /// Discovers models from a provider's models endpoint. The parser accepts
    /// common OpenAI-compatible shapes and simple { models: [...] } responses.
    /// </summary>
    public sealed class ProviderModelDiscovery
    {
        private readonly HttpClient _http;

        public ProviderModelDiscovery(HttpClient? httpClient = null)
        {
            _http = httpClient ?? new HttpClient();
            _http.Timeout = TimeSpan.FromSeconds(30);
        }

        public async Task<IReadOnlyList<DiscoveredModel>> LoadAsync(
            string modelsUrl,
            string apiKey,
            string authHeaderName = "Authorization",
            string authScheme = "Bearer ",
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(modelsUrl))
                throw new ArgumentException("O endpoint de modelos não foi configurado.", nameof(modelsUrl));

            using var request = new HttpRequestMessage(HttpMethod.Get, modelsUrl.Trim());
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                string header = string.IsNullOrWhiteSpace(authHeaderName) ? "Authorization" : authHeaderName;
                string value = (authScheme ?? string.Empty) + apiKey.Trim();
                request.Headers.TryAddWithoutValidation(header, value);
            }
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using HttpResponseMessage response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
            string body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"A API de modelos respondeu {(int)response.StatusCode} ({response.ReasonPhrase}).");

            return Parse(body);
        }

        public static IReadOnlyList<DiscoveredModel> Parse(string json)
        {
            using JsonDocument document = JsonDocument.Parse(json);
            JsonElement root = document.RootElement;
            JsonElement array;

            if (root.ValueKind == JsonValueKind.Array) array = root;
            else if (root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array) array = data;
            else if (root.TryGetProperty("models", out var models) && models.ValueKind == JsonValueKind.Array) array = models;
            else return Array.Empty<DiscoveredModel>();

            var result = new List<DiscoveredModel>();
            foreach (JsonElement item in array.EnumerateArray())
            {
                string id = GetString(item, "id") ?? GetString(item, "name") ?? GetString(item, "model") ?? string.Empty;
                if (string.IsNullOrWhiteSpace(id)) continue;
                result.Add(new DiscoveredModel
                {
                    Id = id.Trim(),
                    OwnedBy = GetString(item, "owned_by") ?? GetString(item, "provider")
                });
            }

            return result
                .GroupBy(m => m.Id, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .OrderBy(m => m.Id, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static string? GetString(JsonElement item, string name) =>
            item.ValueKind == JsonValueKind.Object && item.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;
    }
}
