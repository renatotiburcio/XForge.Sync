# 11 — Advanced Usage

## Custom Transport

```csharp
public class GrpcSyncTransport : ISyncTransport
{
    public async Task<SyncResponse> SendAsync(SyncRequest request, CancellationToken ct)
    {
        // Implement via gRPC
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
        // Merge logic
        return new ResolvedChange(merged, ResolutionStrategy.Merged);
    }
}
```

## Sync Interceptors

```csharp
public class AuditingSyncInterceptor : ISyncInterceptor
{
    public async Task OnBeforeSyncAsync(SyncContext context, CancellationToken ct)
    {
        _logger.LogInformation("Starting sync: {Count} entities", context.PendingChanges);
    }
}
```

---

<div align="center">

**Next:** [Enterprise Best Practices →](enterprise-best-practices.md)

</div>
