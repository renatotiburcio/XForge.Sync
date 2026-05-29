# 14 — Testing

## Testes Unitários com InMemory Store

```csharp
public class ChangeTrackerTests
{
    [Fact]
    public async Task TrackAsync_ShouldRecordChange()
    {
        var store = new InMemoryChangeStore();
        var tracker = new ChangeTracker(store);

        await tracker.TrackAsync(new Order { Id = 1 }, ChangeType.Created);

        var pending = await tracker.GetPendingChangesAsync();
        Assert.Single(pending);
        Assert.Equal(ChangeType.Created, pending[0].ChangeType);
    }
}
```

## Testes de Conflict Resolution

```csharp
[Fact]
public void LastWriteWins_ShouldReturnNewerChange()
{
    var resolver = new LastWriteWinsResolver();
    var local = new Change { Timestamp = DateTime.UtcNow };
    var remote = new Change { Timestamp = DateTime.UtcNow.AddMinutes(5) };

    var result = resolver.Resolve(local, remote);
    Assert.Equal(remote, result);
}
```

## Testes de Integração

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

**Próximo:** [Performance →](performance.md)

</div>
