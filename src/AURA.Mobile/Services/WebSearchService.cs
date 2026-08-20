using System.Threading;
using System.Threading.Tasks;
using AURA.Core.Abstractions;
using AURA.Mobile.Diagnostics;

namespace AURA.Mobile.Services
{
    /// <summary>
    /// Implementação do IWebSearch usando WebSearchAnswer
    /// </summary>
    public sealed class WebSearchService : IWebSearch
    {
        public async Task<string> SearchAsync(string query, CancellationToken ct = default)
        {
            return await WebSearchAnswer.SearchAsync(query, ct);
        }

        public async Task<string> SearchWithRefinementAsync(string query, CancellationToken ct = default)
        {
            return await WebSearchAnswer.SearchWithRefinementAsync(query, ct);
        }
    }
}
