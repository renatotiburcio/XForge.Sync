# 12 — Enterprise Best Practices

## Always Sync After Reconnect

```csharp
app.Lifetime.ApplicationStarted.Register(async () =>
{
    if (await connectivity.IsConnectedAsync())
        await engine.SyncAsync();
});
```

## Retry with Backoff

```csharp
cfg.UseRetry(options =>
{
    options.MaxRetries = 5;
    options.BackoffType = RetryBackoffType.Exponential;
    options.BaseDelay = TimeSpan.FromSeconds(1);
});
```

## Test Conflict Resolution

```csharp
[Fact]
public void LastWriteWins_ShouldReturnNewerChange()
{
    var resolver = new LastWriteWinsResolver();
    var local = new Change { Timestamp = DateTime.UtcNow };
    var remote = new Change { Timestamp = DateTime.UtcNow.AddMinutes(5) };
    Assert.Equal(remote, resolver.Resolve(local, remote));
}
```

---

<div align="center">

**Next:** [Integration Examples →](integration-examples.md)

</div>
