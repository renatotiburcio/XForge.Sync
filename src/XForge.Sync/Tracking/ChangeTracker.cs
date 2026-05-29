using System.Collections.Concurrent;

namespace XForge.Sync.Tracking;

/// <summary>
/// In-memory implementation of <see cref="IChangeTracker"/>.
/// Tracks entity changes for synchronization using thread-safe collections.
/// </summary>
public sealed class ChangeTracker : IChangeTracker
{
    private readonly ConcurrentDictionary<string, TrackedChange> _pendingChanges = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public Task TrackChangeAsync<T>(T entity, ChangeType changeType, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        string entityType = typeof(T).Name;
        string key = $"{entityType}:{GetEntityKey(entity)}";

        TrackedChange change = new()
        {
            Key = key,
            ChangeType = changeType,
            EntityType = entityType,
            Data = System.Text.Json.JsonSerializer.Serialize(entity),
            Version = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            TrackedAt = DateTime.UtcNow
        };

        _pendingChanges[key] = change;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<TrackedChange>> GetPendingChangesAsync(CancellationToken ct = default)
    {
        List<TrackedChange> changes = [.. _pendingChanges.Values.OrderBy(c => c.TrackedAt)];

        return Task.FromResult<IReadOnlyList<TrackedChange>>(changes);
    }

    /// <inheritdoc />
    public Task MarkSyncedAsync(IEnumerable<TrackedChange> changes, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(changes);

        foreach (TrackedChange change in changes)
        {
            _pendingChanges.TryRemove(change.Key, out _);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ClearAsync(CancellationToken ct = default)
    {
        _pendingChanges.Clear();
        return Task.CompletedTask;
    }

    private static string GetEntityKey<T>(T entity)
    {
        // Try to find an Id property via reflection
        System.Reflection.PropertyInfo? idProp = typeof(T).GetProperty("Id");
        if (idProp is not null)
        {
            object? value = idProp.GetValue(entity);
            return value?.ToString() ?? Guid.NewGuid().ToString();
        }

        return entity!.GetHashCode().ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}
