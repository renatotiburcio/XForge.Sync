namespace XForge.Sync.Queue;

/// <summary>
/// A queue for pending sync operations, enabling offline-first behavior.
/// Operations are enqueued locally and dequeued when sync is triggered.
/// </summary>
public interface ISyncQueue
{
    /// <summary>
    /// Enqueues a sync operation.
    /// </summary>
    /// <param name="operation">The operation to enqueue.</param>
    /// <param name="ct">Cancellation token.</param>
    Task EnqueueAsync(SyncOperation operation, CancellationToken ct = default);

    /// <summary>
    /// Dequeues up to the specified number of operations.
    /// </summary>
    /// <param name="count">Maximum number of operations to dequeue.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The dequeued operations.</returns>
    Task<IReadOnlyList<SyncOperation>> DequeueAsync(int count, CancellationToken ct = default);

    /// <summary>
    /// Gets the number of pending operations in the queue.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The pending operation count.</returns>
    Task<int> GetPendingCountAsync(CancellationToken ct = default);

    /// <summary>
    /// Re-enqueues a failed operation for retry.
    /// </summary>
    /// <param name="operation">The operation to retry.</param>
    /// <param name="ct">Cancellation token.</param>
    Task RequeueAsync(SyncOperation operation, CancellationToken ct = default);
}
