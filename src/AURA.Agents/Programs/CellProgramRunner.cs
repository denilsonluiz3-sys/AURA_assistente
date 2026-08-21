using System;
using System.Threading;
using System.Threading.Tasks;
using AURA.Abstractions;
using AURA.Core.Logging;

namespace AURA.Agents.Programs;

public sealed class CellProgramRunner
{
    private readonly ILogger _logger;

    public CellProgramRunner(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<CellProgramResult> RunAsync(
        IAuraCellProgram program,
        IAuraCellContext context,
        CancellationToken ct = default)
    {
        if (program is null) throw new ArgumentNullException(nameof(program));
        if (context is null) throw new ArgumentNullException(nameof(context));

        try
        {
            ct.ThrowIfCancellationRequested();
            _logger.Info($"Executando programa '{program.Name}' (programa {context.CellId})");
            return await program.ExecuteAsync(context, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            _logger.Warning($"Programa '{program.Name}' cancelado");
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error($"Falha no programa '{program.Name}': {ex.Message}");
            return CellProgramResult.Fail(ex.Message);
        }
    }
}
