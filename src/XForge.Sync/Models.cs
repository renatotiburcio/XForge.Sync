namespace XForge.Sync;

/// <summary>
/// Metadata associated with a locally stored item for sync tracking.
/// </summary>
public sealed record SyncMetadata
{
    /// <summary>Gets the unique key identifying the item.</summary>
    public required string Key { get; init; }

    /// <summary>Gets the local version number (incremented on each change).</summary>
    public long Version { get; init; }

    /// <summary>Gets an optional checksum for integrity verification.</summary>
    public string? Checksum { get; init; }

    /// <summary>Gets a value indicating whether the item has unsynced changes.</summary>
    public bool IsDirty { get; init; }

    /// <summary>Gets the timestamp of the last local modification.</summary>
    public DateTime LastModified { get; init; }

    /// <summary>Gets the timestamp of the last successful sync, or null if never synced.</summary>
    public DateTime? LastSynced { get; init; }
}

/// <summary>
/// Represents a tracked change waiting to be synced.
/// </summary>
public sealed record TrackedChange
{
    /// <summary>Gets the unique key identifying the changed item.</summary>
    public required string Key { get; init; }

    /// <summary>Gets the type of change.</summary>
    public ChangeType ChangeType { get; init; }

    /// <summary>Gets the entity type name.</summary>
    public required string EntityType { get; init; }

    /// <summary>Gets the serialized entity data at the time of the change.</summary>
    public string? Data { get; init; }

    /// <summary>Gets the version of the item when the change was tracked.</summary>
    public long Version { get; init; }

    /// <summary>Gets the timestamp when the change was tracked.</summary>
    public DateTime TrackedAt { get; init; }
}

/// <summary>
/// Represents a pending sync operation in the queue.
/// </summary>
public sealed record SyncOperation
{
    /// <summary>Gets the unique operation identifier.</summary>
    public required string OperationId { get; init; }

    /// <summary>Gets the key of the item being synced.</summary>
    public required string Key { get; init; }

    /// <summary>Gets the type of change.</summary>
    public ChangeType ChangeType { get; init; }

    /// <summary>Gets the entity type name.</summary>
    public required string EntityType { get; init; }

    /// <summary>Gets the serialized entity data.</summary>
    public string? Data { get; init; }

    /// <summary>Gets the version of the item.</summary>
    public long Version { get; init; }

    /// <summary>Gets the number of retry attempts.</summary>
    public int RetryCount { get; init; }

    /// <summary>Gets the timestamp when the operation was enqueued.</summary>
    public DateTime EnqueuedAt { get; init; }
}

/// <summary>
/// Represents the context for sync policy decisions.
/// </summary>
public sealed record SyncContext
{
    /// <summary>Gets the number of pending changes.</summary>
    public int PendingChanges { get; init; }

    /// <summary>Gets a value indicating whether the device is currently online.</summary>
    public bool IsOnline { get; init; }

    /// <summary>Gets the time since the last successful sync.</summary>
    public TimeSpan TimeSinceLastSync { get; init; }

    /// <summary>Gets the battery level percentage, or null if unknown.</summary>
    public int? BatteryLevel { get; init; }
}
