using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace AURA.Mobile.Diagnostics;

/// <summary>
/// Resposta sem API key via busca web (Bing).
/// </summary>
public static class WebSearchAnswer
{
    private static readonly HttpClient Http = new()
    {
        Timeout = TimeSpan.FromSeconds(20),
    };

    // Em string verbatim (@"..."), aspas literais usam "" — nunca \"
    private const string BingAlgoPattern =
        @"<li\s+class=""b_algo""[\s\S]*?<h2[^>]*>\s*<a[^>]*href=""([^""]+)""[^>]*>([\s\S]*?)</a>[\s\S]*?(?:<p>|class=""b_caption""[\s\S]*?<p[^>]*>)([\s\S]*?)</p>";

    private const string BingTitlePattern =
        @"<h2[^>]*>\s*<a[^>]*href=""([^""]+)""[^>]*>([\s\S]*?)</a>";

    static WebSearchAnswer()
    {
        Http.DefaultRequestHeaders.TryAddWithoutValidation(
            "User-Agent",
            "Mozilla/5.0 (Linux; Android 13) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Mobile Safari/537.36");
        Http.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Language", "pt-BR,pt;q=0.9,en;q=0.8");
    }

    public static async Task<string> SearchAsync(string query, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return "Digite uma pergunta para buscar.";

        string q = query.Trim();
        try
        {
            string? bing = await TryBingAsync(q, ct).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(bing))
                return bing;

            string? ddg = await TryDuckDuckGoAsync(q, ct).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(ddg))
                return ddg;

            return "Não foi possível obter resultados da web agora. " +
                   "Verifique a conexão ou configure uma chave de API / Ollama.";
        }
        catch (Exception ex)
        {
            AuraLog.Exception("WebSearchAnswer.SearchAsync", ex);
            return "Erro na busca web: " + ex.Message;
        }
    }

    private static async Task<string?> TryBingAsync(string query, CancellationToken ct)
    {
        string url = "https://www.bing.com/search?q=" + Uri.EscapeDataString(query);

        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        using HttpResponseMessage resp = await Http.SendAsync(req, ct).ConfigureAwait(false);
        if (!resp.IsSuccessStatusCode)
            return null;

        string html = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(html) || html.Length < 200)
            return null;

        var results = new List<(string Title, string Snippet, string Link)>();

        foreach (Match m in Regex.Matches(html, BingAlgoPattern, RegexOptions.IgnoreCase))
        {
            string link = WebUtility.HtmlDecode(m.Groups[1].Value.Trim());
            string title = StripTags(m.Groups[2].Value);
            string snippet = StripTags(m.Groups[3].Value);
            if (string.IsNullOrWhiteSpace(title))
                continue;
            results.Add((title, snippet, link));
            if (results.Count >= 5)
                break;
        }

        if (results.Count == 0)
        {
            foreach (Match m in Regex.Matches(html, BingTitlePattern, RegexOptions.IgnoreCase))
            {
                string link = WebUtility.HtmlDecode(m.Groups[1].Value.Trim());
                string title = StripTags(m.Groups[2].Value);
                if (string.IsNullOrWhiteSpace(title) ||
                    link.Contains("javascript:", StringComparison.OrdinalIgnoreCase))
                    continue;
                results.Add((title, "", link));
                if (results.Count >= 5)
                    break;
            }
        }

        if (results.Count == 0)
            return null;

        var sb = new StringBuilder();
        sb.AppendLine("Resultados da web (Bing) — sem chave de API:");
        sb.AppendLine();
        int i = 1;
        foreach (var r in results)
        {
            sb.Append(i++).Append(". ").AppendLine(r.Title);
            if (!string.IsNullOrWhiteSpace(r.Snippet))
                sb.Append("   ").AppendLine(r.Snippet);
            if (!string.IsNullOrWhiteSpace(r.Link))
                sb.Append("   ").AppendLine(r.Link);
            sb.AppendLine();
        }
        return sb.ToString().Trim();
    }

    private static async Task<string?> TryDuckDuckGoAsync(string query, CancellationToken ct)
    {
        string url = "https://api.duckduckgo.com/?q=" + Uri.EscapeDataString(query) +
                     "&format=json&no_html=1&skip_disambig=1";
        using HttpResponseMessage resp = await Http.GetAsync(url, ct).ConfigureAwait(false);
        if (!resp.IsSuccessStatusCode)
            return null;

        string json = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        var root = doc.RootElement;

        string abstractText = root.TryGetProperty("AbstractText", out var at)
            ? at.GetString() ?? ""
            : "";
        string abstractUrl = root.TryGetProperty("AbstractURL", out var au)
            ? au.GetString() ?? ""
            : "";
        string heading = root.TryGetProperty("Heading", out var h)
            ? h.GetString() ?? ""
            : "";

        var sb = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(abstractText))
        {
            sb.AppendLine("Resumo (DuckDuckGo):");
            if (!string.IsNullOrWhiteSpace(heading))
                sb.AppendLine(heading);
            sb.AppendLine(abstractText);
            if (!string.IsNullOrWhiteSpace(abstractUrl))
                sb.AppendLine(abstractUrl);
            sb.AppendLine();
        }

        if (root.TryGetProperty("RelatedTopics", out var topics) &&
            topics.ValueKind == System.Text.Json.JsonValueKind.Array)
        {
            int n = 0;
            foreach (var t in topics.EnumerateArray())
            {
                if (!t.TryGetProperty("Text", out var textEl))
                    continue;
                string text = textEl.GetString() ?? "";
                string firstUrl = "";
                if (t.TryGetProperty("FirstURL", out var fu))
                    firstUrl = fu.GetString() ?? "";
                if (string.IsNullOrWhiteSpace(text))
                    continue;
                if (n == 0)
                    sb.AppendLine("Tópicos relacionados:");
                n++;
                sb.Append(n).Append(". ").AppendLine(text);
                if (!string.IsNullOrWhiteSpace(firstUrl))
                    sb.Append("   ").AppendLine(firstUrl);
                if (n >= 5)
                    break;
            }
        }

        string result = sb.ToString().Trim();
        return string.IsNullOrWhiteSpace(result) ? null : result;
    }

    private static string StripTags(string html)
    {
        if (string.IsNullOrEmpty(html))
            return "";
        string t = Regex.Replace(html, "<[^>]+>", " ");
        t = WebUtility.HtmlDecode(t);
        t = Regex.Replace(t, @"\s+", " ").Trim();
        return t;
    }
}
