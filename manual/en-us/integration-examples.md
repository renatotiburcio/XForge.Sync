# 13 — Integration Examples

## Blazor WebAssembly with IndexedDB

```csharp
builder.Services.AddXForgeSync(cfg =>
{
    cfg.UseIndexedDB("MyAppDB");
    cfg.UseHttp("https://api.example.com/sync");
});
```

## MAUI/WinUI with SQLite

```csharp
var dbPath = Path.Combine(FileSystem.AppDataDirectory, "sync.db");
builder.Services.AddXForgeSync(cfg =>
{
    cfg.UseSqlite(dbPath);
    cfg.UseHttp("https://api.example.com/sync");
});
```

## SignalR Real-Time

```csharp
builder.Services.AddXForgeSync(cfg =>
{
    cfg.UseSqlite("./local.db");
    cfg.UseSignalR("https://api.example.com/synchub");
});
```

## XForge.MediatR Integration

```csharp
public class SyncCommandHandler : IRequestHandler<SyncCommand, SyncResult>
{
    private readonly ISyncEngine _engine;
    public async ValueTask<SyncResult> Handle(SyncCommand req, CancellationToken ct)
        => await _engine.SyncAsync(ct);
}
```

---

<div align="center">

**Next:** [Testing →](testing.md)

</div>
