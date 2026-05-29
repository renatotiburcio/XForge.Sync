# 06 — Quick Start

## Minimal SQLite Example (5 minutes)

```bash
dotnet new console -n MySync
cd MySync
dotnet add package XForge.Sync
dotnet add package XForge.Sync.Sqlite
```

```csharp
using XForge.Sync;
using XForge.Sync.Sqlite;

var store = new SqliteChangeStore("./sync.db");
var tracker = new ChangeTracker(store);
var engine = new SyncEngine(store, httpTransport);

var order = new Order { Id = 1, Status = "Pending" };
await tracker.TrackAsync(order, ChangeType.Modified);

var result = await engine.SyncAsync();
Console.WriteLine($"Synced: {result.SyncedCount}");
Console.WriteLine($"Conflicts: {result.ConflictCount}");
```

## ASP.NET Core Example

```csharp
builder.Services.AddXForgeSync(cfg =>
{
    cfg.UseSqlite("./sync.db");
    cfg.UseHttp("https://api.example.com/sync");
    cfg.UseConflictResolver<LastWriteWinsResolver>();
});
```

---

<div align="center">

**Next:** [Configuration →](configuration.md)

</div>
