# 06 — Quick Start

## Exemplo Mínimo com SQLite (5 minutos)

### 1. Criar o projeto

```bash
dotnet new console -n MeuSync
cd MeuSync
dotnet add package XForge.Sync
dotnet add package XForge.Sync.Sqlite
```

### 2. Configurar o ChangeTracker

```csharp
using XForge.Sync;
using XForge.Sync.Sqlite;

var store = new SqliteChangeStore("./sync.db");
var tracker = new ChangeTracker(store);
var engine = new SyncEngine(store, httpTransport);
```

### 3. Rastrear e sincronizar

```csharp
// Rastrear alteração
var order = new Order { Id = 1, Status = "Pending" };
await tracker.TrackAsync(order, ChangeType.Modified);

// Sincronizar
var result = await engine.SyncAsync();
Console.WriteLine($"Sincronizados: {result.SyncedCount}");
Console.WriteLine($"Conflitos: {result.ConflictCount}");
```

## Exemplo com ASP.NET Core

```csharp
builder.Services.AddXForgeSync(cfg =>
{
    cfg.UseSqlite("./sync.db");
    cfg.UseHttp("https://api.example.com/sync");
    cfg.UseConflictResolver<LastWriteWinsResolver>();
});

app.MapPost("/sync", async (ISyncEngine engine) =>
{
    var result = await engine.SyncAsync();
    return Results.Ok(result);
});
```

---

<div align="center">

**Próximo:** [Configuração →](configuration.md)

</div>
