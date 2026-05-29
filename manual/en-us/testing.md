# 14 — Testing

## Unit Tests with InMemory Store

```csharp
[Fact]
public async Task TrackAsync_ShouldRecordChange()
{
    var store = new InMemoryChangeStore();
    var tracker = new ChangeTracker(store);
    await tracker.TrackAsync(new Order { Id = 1 }, ChangeType.Created);
    var pending = await tracker.GetPendingChangesAsync();
    Assert.Single(pending);
}
```

## Integration Tests

```csharp
public class SyncIntegrationTests : IAsyncLifetime
{
    private SqliteChangeStore _store;
    private SyncEngine _engine;

    public async Task InitializeAsync()
    {
        _store = new SqliteChangeStore(":memory:");
        await _store.InitializeAsync();
        _engine = new SyncEngine(_store, new InMemoryTransport());
    }

    [Fact]
    public async Task FullSync_ShouldSyncPendingChanges()
    {
        var tracker = new ChangeTracker(_store);
        await tracker.TrackAsync(new Order { Id = 1 }, ChangeType.Created);
        var result = await _engine.SyncAsync();
        Assert.Equal(1, result.SyncedCount);
    }

    public async Task DisposeAsync() => await _store.DisposeAsync();
}
```

---

<div align="center">

**Next:** [Performance →](performance.md)

</div>
