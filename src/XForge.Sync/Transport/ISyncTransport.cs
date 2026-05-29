namespace XForge.Sync.Transport;

/// <summary>
/// Abstracts the transport layer for sync communication between client and server.
/// </summary>
public interface ISyncTransport
{
    /// <summary>
    /// Sends a sync request to the server and returns the response.
    /// </summary>
    /// <param name="request">The sync request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The sync response from the server.</returns>
    Task<SyncResponse> SendAsync(SyncRequest request, CancellationToken ct = default);

    /// <summary>
    /// Checks whether the transport is currently available (e.g., server reachable).
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the transport is available; otherwise, false.</returns>
    Task<bool> IsAvailableAsync(CancellationToken ct = default);
}
