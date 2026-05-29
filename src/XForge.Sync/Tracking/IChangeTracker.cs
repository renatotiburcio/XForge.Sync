namespace XForge.Sync.Tracking;

/// <summary>
/// Tracks local entity changes for synchronization.
/// </summary>
public interface IChangeTracker
{
    /// <summary>
    /// Tracks a change to an entity.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entity">The changed entity.</param>
    /// <param name="changeType">The type of change.</param>
    /// <param name="ct">Cancellation token.</param>
    Task TrackChangeAsync<T>(T entity, ChangeType changeType, CancellationToken ct = default);

    /// <summary>
    /// Gets all pending (unsynced) changes.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The list of pending changes.</returns>
    Task<IReadOnlyList<TrackedChange>> GetPendingChangesAsync(CancellationToken ct = default);

    /// <summary>
    /// Marks the specified changes as synced.
    /// </summary>
    /// <param name="changes">The changes to mark as synced.</param>
    /// <param name="ct">Cancellation token.</param>
    Task MarkSyncedAsync(IEnumerable<TrackedChange> changes, CancellationToken ct = default);

    /// <summary>
    /// Clears all tracked changes.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    Task ClearAsync(CancellationToken ct = default);
}
