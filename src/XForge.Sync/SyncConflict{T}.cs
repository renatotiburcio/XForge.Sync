namespace XForge.Sync;

/// <summary>
/// Represents a conflict between local and remote versions of an entity.
/// </summary>
/// <typeparam name="T">The type of the conflicting entity.</typeparam>
public sealed record SyncConflict<T>
{
    /// <summary>Gets the local version of the entity.</summary>
    public required T Local { get; init; }

    /// <summary>Gets the remote version of the entity.</summary>
    public required T Remote { get; init; }

    /// <summary>Gets the base version (common ancestor), if available.</summary>
    public T? Base { get; init; }

    /// <summary>Gets the key of the conflicting entity.</summary>
    public required string Key { get; init; }

    /// <summary>Gets the local version number.</summary>
    public long LocalVersion { get; init; }

    /// <summary>Gets the remote version number.</summary>
    public long RemoteVersion { get; init; }

    /// <summary>Gets the timestamp of the local modification.</summary>
    public DateTime LocalModified { get; init; }

    /// <summary>Gets the timestamp of the remote modification.</summary>
    public DateTime RemoteModified { get; init; }
}
