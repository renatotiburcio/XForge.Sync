# 10 — Intermediate Usage

## Delta Sync

Only differences are transmitted:

```csharp
var change = await tracker.TrackAsync(order, ChangeType.Modified);
// change.Delta contains only modified fields
```

## Batch Sync

```csharp
var options = new SyncOptions { BatchSize = 50 };
var result = await engine.SyncAsync(options);
```

## Bidirectional Sync

```csharp
var uploadResult = await engine.PushAsync();
var downloadResult = await engine.PullAsync();
// Or both
var syncResult = await engine.SyncAsync();
```

---

<div align="center">

**Next:** [Advanced Usage →](advanced-usage.md)

</div>
