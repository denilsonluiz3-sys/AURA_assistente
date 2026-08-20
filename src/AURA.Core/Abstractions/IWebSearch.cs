using System.Threading;
using System.Threading.Tasks;

namespace AURA.Core.Abstractions
{
    /// <summary>
    /// Interface para serviços de busca web.
    /// Desacopla AURA.AI da implementação concreta de busca.
    /// </summary>
    public interface IWebSearch
    {
        /// <summary>
        /// Busca na web e retorna os resultados como texto.
        /// </summary>
        Task<string> SearchAsync(string query, CancellationToken ct = default);

        /// <summary>
        /// Busca com refinamento automático (tenta refinar a consulta até
        /// obter um resultado útil).
        /// </summary>
        Task<string> SearchWithRefinementAsync(string query, CancellationToken ct = default);
    }
}