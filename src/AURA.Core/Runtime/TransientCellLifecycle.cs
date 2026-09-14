using System.Collections.Concurrent;

namespace AURA.Core.Runtime;

/// <summary>Executa células efêmeras em diretório temporário, sempre removido ao concluir.</summary>
public sealed class TransientCellLifecycle : IAsyncDisposable
{
    private readonly string _root;
    private readonly ConcurrentDictionary<string, string> _active = new(StringComparer.Ordinal);
    private int _disposed;

    public TransientCellLifecycle(string? root = null)
    {
        _root = Path.Combine(root ?? Path.GetTempPath(), "aura-cells", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    public async Task<T> RunAsync<T>(string cellId, Func<string, CancellationToken, Task<T>> operation, CancellationToken ct = default)
    {
        if (Volatile.Read(ref _disposed) != 0) throw new ObjectDisposedException(nameof(TransientCellLifecycle));
        if (string.IsNullOrWhiteSpace(cellId)) throw new ArgumentException("CellId obrigatório.", nameof(cellId));
        ArgumentNullException.ThrowIfNull(operation);
        string safe = new string(cellId.Where(c => char.IsLetterOrDigit(c) || c is '-' or '_').ToArray());
        if (safe.Length == 0) throw new ArgumentException("CellId inválido.", nameof(cellId));
        string dir = Path.Combine(_root, safe + "-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        _active[cellId] = dir;
        try { return await operation(dir, ct).ConfigureAwait(false); }
        finally { _active.TryRemove(cellId, out _); TryDelete(dir); }
    }

    public void Cleanup() { foreach (var dir in _active.Values) TryDelete(dir); TryDelete(_root); }
    public ValueTask DisposeAsync() { if (Interlocked.Exchange(ref _disposed, 1) == 0) Cleanup(); return ValueTask.CompletedTask; }
    private static void TryDelete(string path) { try { if (Directory.Exists(path)) Directory.Delete(path, true); } catch { } }
}
