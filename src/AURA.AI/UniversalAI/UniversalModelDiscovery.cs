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

        if (provider.ModelsUrl.Contains("openrouter.ai", StringComparison.OrdinalIgnoreCase))
        {
            request.Headers.TryAddWithoutValidation("HTTP-Referer", "https://github.com/denilsonluiz3-sys/AURA_assistente");
            request.Headers.TryAddWithoutValidation("X-Title", "AURA Assistente");
        }

        using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(
                $"Falha ao carregar modelos de {provider.Name}: {(int)response.StatusCode} {response.ReasonPhrase}. {body[..Math.Min(body.Length, 500)]}");

        return Parse(provider.Id, body);
    }

    /// <summary>
    /// Free primeiro (OpenRouter :free ou pricing 0). freeOnly=true descarta pagos.
    /// </summary>
    public static IReadOnlyList<UniversalModel> PrioritizeFree(
        IEnumerable<UniversalModel> models, int max = 300, bool freeOnly = false)
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
        var paid = freeOnly
            ? new List<UniversalModel>()
            : list
                .Where(m => !IsFree(m))
                .OrderBy(m => m.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToList();

        return free.Concat(paid).Take(Math.Max(20, max)).ToArray();
    }

    public static bool IsFree(UniversalModel m)
    {
        var id = m.Id ?? string.Empty;
        if (id.Contains(":free", StringComparison.OrdinalIgnoreCase))
            return true;
        if (id.Equals("openrouter/free", StringComparison.OrdinalIgnoreCase))
            return true;
        // Display marcado (pricing zero no parse)
        if ((m.DisplayName ?? string.Empty).Contains("(free)", StringComparison.OrdinalIgnoreCase))
            return true;
        return false;
    }

    /// <summary>Sugestões offline quando a API de models falha ou está vazia.</summary>
    public static IReadOnlyList<UniversalModel> FallbackSuggestions(string providerId)
    {
        var id = (providerId ?? string.Empty).Trim().ToLowerInvariant();
        if (id.Contains("openrouter"))
        {
            // Catálogo free típico OpenRouter (set/2026) — IDs reais com :free
            string[] free =
            {
                "openrouter/free",
                "meta-llama/llama-3.3-70b-instruct:free",
                "meta-llama/llama-3.2-3b-instruct:free",
                "google/gemma-4-31b-it:free",
                "google/gemma-4-26b-a4b-it:free",
                "qwen/qwen3-coder:free",
                "qwen/qwen3-next-80b-a3b-instruct:free",
                "openai/gpt-oss-120b:free",
                "openai/gpt-oss-20b:free",
                "nvidia/nemotron-3-nano-30b-a3b:free",
                "nvidia/nemotron-3-ultra-550b-a55b:free",
                "cohere/north-mini-code:free",
                "nousresearch/hermes-3-llama-3.1-405b:free",
                "cognitivecomputations/dolphin-mistral-24b-venice-edition:free",
                "liquid/lfm-2.5-1.2b-instruct:free",
                "minimax/minimax-m3:free",
                "poolside/laguna-xs.2:free",
                "deepseek/deepseek-r1:free",
                "deepseek/deepseek-chat-v3-0324:free"
            };
            return free.Select(x => new UniversalModel(x, x.Contains(":free") || x.EndsWith("/free") ? x + " (free)" : x, "openrouter")).ToArray();
        }

        if (id.Contains("deepseek"))
        {
            // Docs oficiais 2026: v4; aliases legados ainda podem responder
            return new[]
            {
                new UniversalModel("deepseek-v4-flash", "deepseek-v4-flash (recomendado)", "deepseek"),
                new UniversalModel("deepseek-v4-pro", "deepseek-v4-pro", "deepseek"),
                new UniversalModel("deepseek-chat", "deepseek-chat (legado)", "deepseek"),
                new UniversalModel("deepseek-reasoner", "deepseek-reasoner (legado)", "deepseek")
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
                var display = value!;
                if (IsPricingFree(item) && !value!.Contains(":free", StringComparison.OrdinalIgnoreCase))
                    display = value + " (free)";
                else if (value.Contains(":free", StringComparison.OrdinalIgnoreCase))
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

        return PrioritizeFree(result, max: 400);
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
