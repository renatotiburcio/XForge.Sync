namespace XForge.Sync.Policy;

/// <summary>
/// Default sync policy (D7 decision: sync when online and has pending changes).
/// </summary>
public sealed class DefaultSyncPolicy : ISyncPolicy
{
    private static readonly TimeSpan DefaultInterval = TimeSpan.FromMinutes(5);
    private const int DefaultMaxRetries = 3;

    /// <inheritdoc />
    public bool ShouldSync(SyncContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Sync if online and has pending changes
        // Sync if online and it's been more than the interval since last sync
        return (context.IsOnline && context.PendingChanges > 0)
            || (context.IsOnline && context.TimeSinceLastSync > GetSyncInterval());
    }

    /// <inheritdoc />
    public TimeSpan GetSyncInterval() => DefaultInterval;

    /// <inheritdoc />
    public int GetMaxRetries() => DefaultMaxRetries;
}
