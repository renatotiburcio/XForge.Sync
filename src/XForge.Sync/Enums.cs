namespace XForge.Sync;

/// <summary>
/// Represents the type of change tracked for an entity.
/// </summary>
public enum ChangeType
{
    /// <summary>A new entity was created.</summary>
    Create,

    /// <summary>An existing entity was updated.</summary>
    Update,

    /// <summary>An entity was deleted.</summary>
    Delete
}

/// <summary>
/// Represents the overall status of a sync operation.
/// </summary>
public enum SyncStatus
{
    /// <summary>No sync has been performed yet.</summary>
    Idle,

    /// <summary>A sync operation is in progress.</summary>
    Syncing,

    /// <summary>The last sync completed successfully.</summary>
    Synced,

    /// <summary>The last sync failed.</summary>
    Failed,

    /// <summary>The device is offline.</summary>
    Offline
}

/// <summary>
/// Represents the outcome of a conflict resolution.
/// </summary>
public enum ConflictResolutionType
{
    /// <summary>Use the local version.</summary>
    UseLocal,

    /// <summary>Use the remote version.</summary>
    UseRemote,

    /// <summary>A merged version was produced.</summary>
    Merged,

    /// <summary>Manual resolution is required.</summary>
    Manual
}
