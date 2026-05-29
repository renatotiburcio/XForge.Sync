using System.Collections.Concurrent;

namespace XForge.Sync.Queue;

/// <summary>
/// In-memory implementation of <see cref="ISyncQueue"/>.
/// Uses a thread-safe queue for pending sync operations.
/// </summary>
public sealed class SyncQueue : ISyncQueue
{
    private readonly ConcurrentQueue<SyncOperation> _queue = new();

    /// <inheritdoc />
    public Task EnqueueAsync(SyncOperation operation, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(operation);
        _queue.Enqueue(operation);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<SyncOperation>> DequeueAsync(int count, CancellationToken ct = default)
    {
        List<SyncOperation> result = new(count);

        for (int i = 0; i < count && _queue.TryDequeue(out SyncOperation? operation); i++)
        {
            result.Add(operation);
        }

        return Task.FromResult<IReadOnlyList<SyncOperation>>(result);
    }

    /// <inheritdoc />
    public Task<int> GetPendingCountAsync(CancellationToken ct = default)
    {
        return Task.FromResult(_queue.Count);
    }

    /// <inheritdoc />
    public Task RequeueAsync(SyncOperation operation, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        SyncOperation retried = operation with { RetryCount = operation.RetryCount + 1 };
        _queue.Enqueue(retried);
        return Task.CompletedTask;
    }
}
