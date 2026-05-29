namespace XForge.Sync.Engine;

/// <summary>
/// The main sync engine that orchestrates push/pull operations between local and remote stores.
/// </summary>
public interface ISyncEngine
{
    /// <summary>
    /// Pushes pending local changes to the remote server.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The result of the push operation.</returns>
    Task<SyncResult> PushAsync(CancellationToken ct = default);

    /// <summary>
    /// Pulls remote changes and applies them locally.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The result of the pull operation.</returns>
    Task<SyncResult> PullAsync(CancellationToken ct = default);

    /// <summary>
    /// Performs a full sync cycle (push then pull).
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The result of the sync operation.</returns>
    Task<SyncResult> SyncAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets the current sync status.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The current sync status.</returns>
    Task<SyncStatus> GetStatusAsync(CancellationToken ct = default);
}
