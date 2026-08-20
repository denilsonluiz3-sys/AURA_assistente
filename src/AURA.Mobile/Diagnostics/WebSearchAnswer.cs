using System.Threading;
using System.Threading.Tasks;
using AURA.Core;

namespace AURA.Mobile.Diagnostics;

/// <summary>
/// Resposta sem API key via busca web (Bing/DuckDuckGo).
/// Delega para <see cref="WebSearchService"/> em AURA.Core para que a mesma
/// lógica seja compartilhada com o CLI e o AURA.AI.
/// </summary>
public static class WebSearchAnswer
{
    private static readonly WebSearchService Service = new();

    public static Task<string> SearchAsync(string query, CancellationToken ct = default)
        => Service.SearchAsync(query, ct);

    public static Task<string> SearchWithRefinementAsync(string query, CancellationToken ct = default)
        => Service.SearchWithRefinementAsync(query, ct);
}