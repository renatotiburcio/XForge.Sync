namespace XForge.Sync;

/// <summary>
/// Represents the result of a sync operation.
/// </summary>
public sealed record SyncResult
{
    /// <summary>Gets a value indicating whether the sync was successful.</summary>
    public bool IsSuccess { get; init; }

    /// <summary>Gets the number of items pushed to the server.</summary>
    public int ItemsPushed { get; init; }

    /// <summary>Gets the number of items pulled from the server.</summary>
    public int ItemsPulled { get; init; }

    /// <summary>Gets the number of conflicts detected.</summary>
    public int ConflictsDetected { get; init; }

    /// <summary>Gets the number of conflicts resolved automatically.</summary>
    public int ConflictsResolved { get; init; }

    /// <summary>Gets the error message if the sync failed.</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>Gets the duration of the sync operation.</summary>
    public TimeSpan Duration { get; init; }

    /// <summary>Gets the timestamp when the sync completed.</summary>
    public DateTime CompletedAt { get; init; }

    /// <summary>Creates a successful sync result.</summary>
    public static SyncResult Success(int pushed, int pulled, int conflictsDetected, int conflictsResolved, TimeSpan duration) =>
        new()
        {
            IsSuccess = true,
            ItemsPushed = pushed,
            ItemsPulled = pulled,
            ConflictsDetected = conflictsDetected,
            ConflictsResolved = conflictsResolved,
            Duration = duration,
            CompletedAt = DateTime.UtcNow
        };

    /// <summary>Creates a failed sync result.</summary>
    public static SyncResult Failure(string errorMessage, TimeSpan duration) =>
        new()
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            Duration = duration,
            CompletedAt = DateTime.UtcNow
        };
}

/// <summary>
/// Represents the resolution of a sync conflict.
/// </summary>
/// <typeparam name="T">The type of the conflicting entity.</typeparam>
public sealed record ConflictResolution<T>
{
    /// <summary>Gets the type of resolution applied.</summary>
    public ConflictResolutionType ResolutionType { get; init; }

    /// <summary>Gets the resolved value.</summary>
    public required T ResolvedValue { get; init; }

    /// <summary>Creates a resolution that uses the local value.</summary>
    public static ConflictResolution<T> UseLocal(T value) =>
        new() { ResolutionType = ConflictResolutionType.UseLocal, ResolvedValue = value };

    /// <summary>Creates a resolution that uses the remote value.</summary>
    public static ConflictResolution<T> UseRemote(T value) =>
        new() { ResolutionType = ConflictResolutionType.UseRemote, ResolvedValue = value };

    /// <summary>Creates a resolution with a merged value.</summary>
    public static ConflictResolution<T> Merge(T value) =>
        new() { ResolutionType = ConflictResolutionType.Merged, ResolvedValue = value };
}

/// <summary>
/// Represents a sync request sent from client to server.
/// </summary>
public sealed record SyncRequest
{
    /// <summary>Gets the client identifier.</summary>
    public required string ClientId { get; init; }

    /// <summary>Gets the last known server version.</summary>
    public long LastServerVersion { get; init; }

    /// <summary>Gets the changes to push to the server.</summary>
    public IReadOnlyList<TrackedChange> Changes { get; init; } = [];
}

/// <summary>
/// Represents a sync response from the server.
/// </summary>
public sealed record SyncResponse
{
    /// <summary>Gets a value indicating whether the sync was successful.</summary>
    public bool IsSuccess { get; init; }

    /// <summary>Gets the current server version.</summary>
    public long ServerVersion { get; init; }

    /// <summary>Gets the changes from the server since the client's last version.</summary>
    public IReadOnlyList<TrackedChange> Changes { get; init; } = [];

    /// <summary>Gets any conflicts detected during push.</summary>
    public IReadOnlyList<SyncConflict<object>> Conflicts { get; init; } = [];

    /// <summary>Gets the error message if the sync failed.</summary>
    public string? ErrorMessage { get; init; }
}
