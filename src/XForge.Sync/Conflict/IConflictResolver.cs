namespace XForge.Sync.Conflict;

/// <summary>
/// Resolves conflicts between local and remote versions of an entity.
/// </summary>
public interface IConflictResolver
{
    /// <summary>
    /// Resolves a sync conflict.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="conflict">The conflict to resolve.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The conflict resolution.</returns>
    Task<ConflictResolution<T>> ResolveAsync<T>(SyncConflict<T> conflict, CancellationToken ct = default);
}
