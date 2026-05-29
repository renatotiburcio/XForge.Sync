# 11 — Uso Avançado

## Custom Transport

```csharp
public class GrpcSyncTransport : ISyncTransport
{
    private readonly SyncService.SyncServiceClient _client;

    public async Task<SyncResponse> SendAsync(SyncRequest request, CancellationToken ct)
    {
        var grpcRequest = MapToGrpc(request);
        var response = await _client.SyncAsync(grpcRequest, cancellationToken: ct);
        return MapFromGrpc(response);
    }
}
```

## Custom Conflict Resolver

```csharp
public class MergeResolver : IConflictResolver
{
    public ResolvedChange Resolve(Change local, Change remote)
    {
        var merged = new Dictionary<string, object>();
        foreach (var key in local.Delta.Keys.Union(remote.Delta.Keys))
        {
            merged[key] = remote.Delta.ContainsKey(key) ? remote.Delta[key] : local.Delta[key];
        }
        return new ResolvedChange(merged, ResolutionStrategy.Merged);
    }
}
```

## Interceptores de Sync

```csharp
public class AuditingSyncInterceptor : ISyncInterceptor
{
    public async Task OnBeforeSyncAsync(SyncContext context, CancellationToken ct)
    {
        _logger.LogInformation("Iniciando sync: {EntityCount} entidades", context.PendingChanges);
    }

    public async Task OnAfterSyncAsync(SyncResult result, CancellationToken ct)
    {
        _logger.LogInformation("Sync concluído: {Synced}, {Conflicts}", result.SyncedCount, result.ConflictCount);
    }
}
```

---

<div align="center">

**Próximo:** [Boas Práticas Enterprise →](enterprise-best-practices.md)

</div>
