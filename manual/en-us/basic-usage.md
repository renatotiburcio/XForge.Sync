# 09 — Basic Usage

## Track Changes

```csharp
await tracker.TrackAsync(newOrder, ChangeType.Created);
await tracker.TrackAsync(updatedOrder, ChangeType.Modified);
await tracker.TrackAsync(deletedOrder, ChangeType.Deleted);
```

## Get Pending Changes

```csharp
var pending = await tracker.GetPendingChangesAsync();
foreach (var change in pending)
    Console.WriteLine($"{change.EntityType} - {change.ChangeType}");
```

## Sync

```csharp
var result = await engine.SyncAsync();
foreach (var conflict in result.Conflicts)
    Console.WriteLine($"Conflict: {conflict.EntityId}");
```

---

<div align="center">

**Next:** [Intermediate Usage →](intermediate-usage.md)

</div>
