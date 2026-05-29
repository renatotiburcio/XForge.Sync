# 12 — Boas Práticas Enterprise

## Sempre Sync após Reconnect

```csharp
app.Lifetime.ApplicationStarted.Register(async () =>
{
    if (await connectivity.IsConnectedAsync())
        await syncEngine.SyncAsync();
});
```

## Retry com Backoff

```csharp
cfg.UseRetry(options =>
{
    options.MaxRetries = 5;
    options.BackoffType = RetryBackoffType.Exponential;
    options.BaseDelay = TimeSpan.FromSeconds(1);
});
```

## Logging de Sync

```csharp
cfg.UseSyncLogger(options =>
{
    options.LogChanges = true;
    options.LogConflicts = true;
    options.LogPerformance = true;
});
```

## Testes de Sync

```csharp
[Fact]
public async Task Sync_WithConflict_ShouldResolveUsingStrategy()
{
    var local = new Change { EntityId = "1", Timestamp = DateTime.UtcNow };
    var remote = new Change { EntityId = "1", Timestamp = DateTime.UtcNow.AddSeconds(5) };

    var resolver = new LastWriteWinsResolver();
    var result = resolver.Resolve(local, remote);

    Assert.Equal(remote.Timestamp, result.Timestamp);
}
```

---

<div align="center">

**Próximo:** [Exemplos de Integração →](integration-examples.md)

</div>
