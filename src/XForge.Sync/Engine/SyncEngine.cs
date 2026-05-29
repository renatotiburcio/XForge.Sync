using Microsoft.Extensions.Logging;
using XForge.Sync.Conflict;
using XForge.Sync.Connectivity;
using XForge.Sync.Policy;
using XForge.Sync.Queue;
using XForge.Sync.Tracking;
using XForge.Sync.Transport;

namespace XForge.Sync.Engine;

/// <summary>
/// Default implementation of <see cref="ISyncEngine"/> that orchestrates
/// push/pull operations using change tracking, transport, and conflict resolution.
/// </summary>
public sealed class SyncEngine(
    ISyncQueue queue,
    ISyncTransport transport,
    IConflictResolver conflictResolver,
    IChangeTracker changeTracker,
    ISyncPolicy policy,
    IConnectivityMonitor connectivity,
    ILogger<SyncEngine> logger) : ISyncEngine
{
    private readonly ISyncQueue _queue = queue ?? throw new ArgumentNullException(nameof(queue));
    private readonly ISyncTransport _transport = transport ?? throw new ArgumentNullException(nameof(transport));
    private readonly IConflictResolver _conflictResolver = conflictResolver ?? throw new ArgumentNullException(nameof(conflictResolver));
    private readonly IChangeTracker _changeTracker = changeTracker ?? throw new ArgumentNullException(nameof(changeTracker));
    private readonly ISyncPolicy _policy = policy ?? throw new ArgumentNullException(nameof(policy));
    private readonly IConnectivityMonitor _connectivity = connectivity ?? throw new ArgumentNullException(nameof(connectivity));
    private readonly ILogger<SyncEngine> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

#if NET9_0_OR_GREATER
    private readonly Lock _lock = new();
#else
    private readonly object _lock = new();
#endif
    private SyncStatus _status = SyncStatus.Idle;

    /// <inheritdoc />
    public Task<SyncStatus> GetStatusAsync(CancellationToken ct = default)
    {
        lock (_lock)
        {
            return Task.FromResult(_status);
        }
    }

    /// <inheritdoc />
    public async Task<SyncResult> PushAsync(CancellationToken ct = default)
    {
        DateTime start = DateTime.UtcNow;

        if (!_connectivity.IsOnline)
        {
            return SyncResult.Failure("Device is offline", DateTime.UtcNow - start);
        }

        SetStatus(SyncStatus.Syncing);

        try
        {
            IReadOnlyList<TrackedChange> pendingChanges = await _changeTracker.GetPendingChangesAsync(ct);

            if (pendingChanges.Count == 0)
            {
                SetStatus(SyncStatus.Synced);
                return SyncResult.Success(0, 0, 0, 0, DateTime.UtcNow - start);
            }

            SyncRequest request = new()
            {
                ClientId = Environment.MachineName,
                LastServerVersion = 0,
                Changes = pendingChanges
            };

            SyncResponse response = await _transport.SendAsync(request, ct);

            if (!response.IsSuccess)
            {
                SetStatus(SyncStatus.Failed);
                return SyncResult.Failure(response.ErrorMessage ?? "Push failed", DateTime.UtcNow - start);
            }

            await _changeTracker.MarkSyncedAsync(pendingChanges, ct);

            int conflictsResolved = 0;
            if (response.Conflicts.Count > 0)
            {
                _logger.LogWarning("Server reported {Count} conflicts during push", response.Conflicts.Count);
                conflictsResolved = response.Conflicts.Count; // Server handled them
            }

            SetStatus(SyncStatus.Synced);
            return SyncResult.Success(
                pendingChanges.Count,
                0,
                response.Conflicts.Count,
                conflictsResolved,
                DateTime.UtcNow - start);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Push failed");
            SetStatus(SyncStatus.Failed);
            return SyncResult.Failure(ex.Message, DateTime.UtcNow - start);
        }
    }

    /// <inheritdoc />
    public async Task<SyncResult> PullAsync(CancellationToken ct = default)
    {
        DateTime start = DateTime.UtcNow;

        if (!_connectivity.IsOnline)
        {
            return SyncResult.Failure("Device is offline", DateTime.UtcNow - start);
        }

        SetStatus(SyncStatus.Syncing);

        try
        {
            SyncRequest request = new()
            {
                ClientId = Environment.MachineName,
                LastServerVersion = 0,
                Changes = []
            };

            SyncResponse response = await _transport.SendAsync(request, ct);

            if (!response.IsSuccess)
            {
                SetStatus(SyncStatus.Failed);
                return SyncResult.Failure(response.ErrorMessage ?? "Pull failed", DateTime.UtcNow - start);
            }

            int conflictsDetected = 0;
            int conflictsResolved = 0;

            if (response.Conflicts.Count > 0)
            {
                conflictsDetected = response.Conflicts.Count;
                foreach (SyncConflict<object> conflict in response.Conflicts)
                {
                    ConflictResolution<object> resolution = await _conflictResolver.ResolveAsync(conflict, ct);
                    conflictsResolved++;
                    _logger.LogInformation("Resolved conflict for key {Key} with {Resolution}",
                        conflict.Key, resolution.ResolutionType);
                }
            }

            SetStatus(SyncStatus.Synced);
            return SyncResult.Success(
                0,
                response.Changes.Count,
                conflictsDetected,
                conflictsResolved,
                DateTime.UtcNow - start);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Pull failed");
            SetStatus(SyncStatus.Failed);
            return SyncResult.Failure(ex.Message, DateTime.UtcNow - start);
        }
    }

    /// <inheritdoc />
    public async Task<SyncResult> SyncAsync(CancellationToken ct = default)
    {
        DateTime start = DateTime.UtcNow;

        if (!_connectivity.IsOnline)
        {
            return SyncResult.Failure("Device is offline", DateTime.UtcNow - start);
        }

        SyncContext context = new()
        {
            PendingChanges = await _queue.GetPendingCountAsync(ct),
            IsOnline = _connectivity.IsOnline,
            TimeSinceLastSync = TimeSpan.Zero
        };

        if (!_policy.ShouldSync(context))
        {
            return SyncResult.Failure("Sync policy declined", DateTime.UtcNow - start);
        }

        SyncResult pushResult = await PushAsync(ct);
        if (!pushResult.IsSuccess)
        {
            return pushResult;
        }

        SyncResult pullResult = await PullAsync(ct);

        return SyncResult.Success(
            pushResult.ItemsPushed,
            pullResult.ItemsPulled,
            pushResult.ConflictsDetected + pullResult.ConflictsDetected,
            pushResult.ConflictsResolved + pullResult.ConflictsResolved,
            DateTime.UtcNow - start);
    }

    private void SetStatus(SyncStatus status)
    {
        lock (_lock)
        {
            _status = status;
        }
    }
}
