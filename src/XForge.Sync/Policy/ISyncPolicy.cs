namespace XForge.Sync.Policy;

/// <summary>
/// Defines sync scheduling and behavior policies.
/// </summary>
public interface ISyncPolicy
{
    /// <summary>
    /// Determines whether a sync should be triggered based on the current context.
    /// </summary>
    /// <param name="context">The current sync context.</param>
    /// <returns>True if sync should be triggered; otherwise, false.</returns>
    bool ShouldSync(SyncContext context);

    /// <summary>
    /// Gets the desired interval between automatic syncs.
    /// </summary>
    /// <returns>The sync interval.</returns>
    TimeSpan GetSyncInterval();

    /// <summary>
    /// Gets the maximum number of retry attempts for failed operations.
    /// </summary>
    /// <returns>The maximum retry count.</returns>
    int GetMaxRetries();
}
