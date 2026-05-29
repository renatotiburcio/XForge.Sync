using System.Text.Json;
using XForge.Sync.AspNetCore;

namespace XForge.Sync.IntegrationTests;

/// <summary>
/// In-memory server-side sync handler for integration testing.
/// Stores entities in-memory and simulates server-side conflict detection.
/// </summary>
public class InMemoryServerSyncHandler : IServerSyncHandler
{
    private readonly Dictionary<string, TrackedChange> _serverStore = new(StringComparer.Ordinal);

    /// <summary>
    /// Gets the number of entities stored on the server.
    /// </summary>
    public int EntityCount => _serverStore.Count;

    /// <summary>
    /// Gets the current server version.
    /// </summary>
    public long CurrentVersion { get; private set; }

    /// <summary>
    /// Pre-populates the server store for testing.
    /// </summary>
    public void Seed(string key, string jsonData, long version = 1)
    {
        _serverStore[key] = new TrackedChange
        {
            Key = key,
            Data = jsonData,
            EntityType = "TestEntity",
            ChangeType = ChangeType.Update,
            TrackedAt = DateTime.UtcNow,
            Version = version
        };
        CurrentVersion = Math.Max(CurrentVersion, version);
    }

    /// <inheritdoc />
    public Task<SyncResponse> HandleSyncAsync(SyncRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        List<TrackedChange> serverChanges = [];
        List<SyncConflict<object>> conflicts = [];

        // Process client push changes
        foreach (TrackedChange change in request.Changes)
        {
            if (_serverStore.TryGetValue(change.Key, out TrackedChange? existing))
            {
                // Server has this entity — check for conflict
                if (existing.Version > request.LastServerVersion)
                {
                    object? localObj = JsonSerializer.Deserialize<object>(change.Data ?? "{}");
                    object? remoteObj = JsonSerializer.Deserialize<object>(existing.Data ?? "{}");

                    conflicts.Add(new SyncConflict<object>
                    {
                        Local = localObj!,
                        Remote = remoteObj!,
                        Key = change.Key,
                        LocalVersion = change.Version,
                        RemoteVersion = existing.Version,
                        LocalModified = change.TrackedAt,
                        RemoteModified = DateTime.UtcNow
                    });
                }
            }

            // Apply change to server
            CurrentVersion++;
            _serverStore[change.Key] = new TrackedChange
            {
                Key = change.Key,
                Data = change.Data,
                EntityType = change.EntityType,
                ChangeType = change.ChangeType,
                TrackedAt = DateTime.UtcNow,
                Version = CurrentVersion
            };
        }

        // Collect server changes since client's last version
        foreach (KeyValuePair<string, TrackedChange> entry in _serverStore)
        {
            if (entry.Value.Version > request.LastServerVersion)
            {
                serverChanges.Add(entry.Value);
            }
        }

        return Task.FromResult(new SyncResponse
        {
            IsSuccess = true,
            ServerVersion = CurrentVersion,
            Changes = serverChanges,
            Conflicts = conflicts
        });
    }
}
