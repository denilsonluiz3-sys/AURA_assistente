using System.Threading;

namespace AURA.Abstractions;

public interface IAuraCellContextFactory
{
    IAuraCellContext Create(string cellId, CancellationToken ct = default);
}
