namespace XForge.Sync.Conflict;

/// <summary>
/// Resolves conflicts using the Last-Write-Wins strategy (D7 decision).
/// Compares LastModified timestamps and selects the newer version.
/// </summary>
public sealed class LastWriteWinsResolver : IConflictResolver
{
    /// <inheritdoc />
    public Task<ConflictResolution<T>> ResolveAsync<T>(SyncConflict<T> conflict, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(conflict);

        // D7 Decision: Last-Write-Wins — compare timestamps
        return conflict.LocalModified >= conflict.RemoteModified
            ? Task.FromResult(ConflictResolution<T>.UseLocal(conflict.Local))
            : Task.FromResult(ConflictResolution<T>.UseRemote(conflict.Remote));
    }
}
