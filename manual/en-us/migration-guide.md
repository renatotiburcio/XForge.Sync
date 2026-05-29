# 22 — Migration Guide

## From Manual Sync

```csharp
// Before: manual dirty tracking
var changes = db.Orders.Where(o => o.IsDirty);
foreach (var order in changes)
    await httpClient.PostAsJsonAsync("/api/sync", order);

// After: XForge.Sync
await tracker.TrackAsync(order, ChangeType.Modified);
var result = await engine.SyncAsync();
```

---

<div align="center">

**Next:** [Contributing →](contributing.md)

</div>
