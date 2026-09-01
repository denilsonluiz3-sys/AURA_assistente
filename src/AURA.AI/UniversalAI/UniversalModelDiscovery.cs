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
            request.Headers.TryAddWithoutValidation(
                provider.AuthHeader,
                string.IsNullOrWhiteSpace(provider.AuthScheme)
                    ? apiKey.Trim()
                    : provider.AuthScheme + " " + apiKey.Trim());

        // OpenRouter: pedir catálogo completo quando possível
        if (provider.ModelsUrl.Contains("openrouter.ai", StringComparison.OrdinalIgnoreCase))
            request.Headers.TryAddWithoutValidation("HTTP-Referer", "https://github.com/denilsonluiz3-sys/AURA_assistente");

        using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(
                $"Falha ao carregar modelos de {provider.Name}: {(int)response.StatusCode} {response.ReasonPhrase}. {body[..Math.Min(body.Length, 500)]}");

        return Parse(provider.Id, body);
    }

    /// <summary>
    /// Free primeiro (OpenRouter :free), depois o resto. Limite alto para o picker.
    /// </summary>
    public static IReadOnlyList<UniversalModel> PrioritizeFree(IEnumerable<UniversalModel> models, int max = 200)
    {
        var list = models
            .Where(m => !string.IsNullOrWhiteSpace(m.Id))
            .GroupBy(m => m.Id, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();

        var free = list
            .Where(IsFree)
            .OrderBy(m => m.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var paid = list
            .Where(m => !IsFree(m))
            .OrderBy(m => m.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return free.Concat(paid).Take(Math.Max(20, max)).ToArray();
    }

    public static bool IsFree(UniversalModel m)
    {
        var id = m.Id ?? string.Empty;
        return id.Contains(":free", StringComparison.OrdinalIgnoreCase)
            || id.EndsWith("/free", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Sugestões offline quando a API de models falha ou está vazia.</summary>
    public static IReadOnlyList<UniversalModel> FallbackSuggestions(string providerId)
    {
        var id = (providerId ?? string.Empty).Trim().ToLowerInvariant();
        if (id.Contains("openrouter"))
        {
            // IDs free comuns no OpenRouter (atualize conforme o catálogo mudar)
            string[] free =
            {
                "deepseek/deepseek-r1:free",
                "deepseek/deepseek-chat-v3-0324:free",
                "deepseek/deepseek-r1-0528:free",
                "google/gemma-3-27b-it:free",
                "google/gemma-3-12b-it:free",
                "google/gemma-3-4b-it:free",
                "meta-llama/llama-3.3-70b-instruct:free",
                "meta-llama/llama-3.2-3b-instruct:free",
                "meta-llama/llama-3.1-8b-instruct:free",
                "qwen/qwen3-4b:free",
                "qwen/qwen2.5-vl-32b-instruct:free",
                "mistralai/mistral-small-3.1-24b-instruct:free",
                "mistralai/mistral-7b-instruct:free",
                "nvidia/llama-3.1-nemotron-ultra-253b-v1:free",
                "openrouter/free"
            };
            return free.Select(x => new UniversalModel(x, x + " (free)", "openrouter")).ToArray();
        }

        if (id.Contains("deepseek"))
        {
            return new[]
            {
                new UniversalModel("deepseek-chat", "deepseek-chat", "deepseek"),
                new UniversalModel("deepseek-reasoner", "deepseek-reasoner", "deepseek")
            };
        }

        if (id.Contains("openai"))
        {
            return new[]
            {
                new UniversalModel("gpt-4o-mini", "gpt-4o-mini", "openai"),
                new UniversalModel("gpt-4o", "gpt-4o", "openai")
            };
        }

        return Array.Empty<UniversalModel>();
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
                if (!item.TryGetProperty("id", out var idEl))
                    continue;
                var value = idEl.GetString();
                if (string.IsNullOrWhiteSpace(value))
                    continue;

                string? owned = item.TryGetProperty("owned_by", out var o) ? o.GetString() : null;
                // OpenRouter: pricing zerado ≈ free (além do sufixo :free)
                var display = value!;
                if (IsPricingFree(item) && !value!.Contains(":free", StringComparison.OrdinalIgnoreCase))
                    display = value + " (free)";

                result.Add(new UniversalModel(value!, display, providerId, owned));
            }
        }
        else if (root.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in root.EnumerateArray())
            {
                var value = item.ValueKind == JsonValueKind.String
                    ? item.GetString()
                    : item.TryGetProperty("name", out var n) ? n.GetString()
                    : item.TryGetProperty("id", out var i) ? i.GetString() : null;
                if (!string.IsNullOrWhiteSpace(value))
                    result.Add(new UniversalModel(value!, value!, providerId));
            }
        }

        return PrioritizeFree(result, max: 300);
    }

    private static bool IsPricingFree(JsonElement item)
    {
        try
        {
            if (!item.TryGetProperty("pricing", out var pricing))
                return false;
            var prompt = pricing.TryGetProperty("prompt", out var p) ? p.GetString() : null;
            var completion = pricing.TryGetProperty("completion", out var c) ? c.GetString() : null;
            return IsZeroPrice(prompt) && IsZeroPrice(completion);
        }
        catch
        {
            return false;
        }
    }

    private static bool IsZeroPrice(string? s)
    {
        if (string.IsNullOrWhiteSpace(s))
            return false;
        if (!decimal.TryParse(s, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var v))
            return false;
        return v == 0;
    }
}
