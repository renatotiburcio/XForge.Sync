namespace XForge.Sync.AspNetCore;

/// <summary>
/// Handles server-side sync operations. Implementations process client changes
/// and return server changes since the client's last known version.
/// </summary>
/// <remarks>
/// Register your implementation in DI before calling <c>MapXForgeSync()</c>.
/// </remarks>
public interface IServerSyncHandler
{
    /// <summary>
    /// Processes a sync request from a client. The implementation should:
    /// 1. Apply incoming changes from the client.
    /// 2. Detect conflicts with server state.
    /// 3. Return server changes since the client's last known version.
    /// </summary>
    /// <param name="request">The sync request from the client.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A sync response with server changes and any conflicts.</returns>
    Task<SyncResponse> HandleSyncAsync(SyncRequest request, CancellationToken ct = default);
}
