namespace XForge.Sync.IntegrationTests;

/// <summary>
/// In-memory sync transport for integration testing.
/// Directly invokes the server handler without HTTP.
/// </summary>
public class InMemorySyncTransport(InMemoryServerSyncHandler handler) : ISyncTransport
{
    private readonly InMemoryServerSyncHandler _handler = handler ?? throw new ArgumentNullException(nameof(handler));

    /// <inheritdoc />
    public async Task<SyncResponse> SendAsync(SyncRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return await _handler.HandleSyncAsync(request, ct);
    }

    /// <inheritdoc />
    public Task<bool> IsAvailableAsync(CancellationToken ct = default)
    {
        return Task.FromResult(true);
    }
}
