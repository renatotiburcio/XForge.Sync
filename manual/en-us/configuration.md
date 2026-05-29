# 07 — Configuration

## Basic Configuration

```csharp
builder.Services.AddXForgeSync(cfg =>
{
    cfg.UseSqlite("./local.db");
    cfg.UseHttp("https://api.example.com/sync");
    cfg.UseConflictResolver<LastWriteWinsResolver>();
});
```

## ChangeTracker Options

| Option | Default | Description |
|--------|---------|-------------|
| `MaxRetries` | 3 | Sync retry count |
| `RetryDelay` | 1s | Delay between retries |
| `BatchSize` | 100 | Items per sync batch |
| `ConflictStrategy` | LastWriteWins | Default conflict strategy |

---

<div align="center">

**Next:** [Architecture →](architecture.md)

</div>
