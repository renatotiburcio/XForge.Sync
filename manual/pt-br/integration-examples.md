# 13 — Exemplos de Integração

## Blazor WebAssembly com IndexedDB

```csharp
builder.Services.AddXForgeSync(cfg =>
{
    cfg.UseIndexedDB("MeuAppDB");
    cfg.UseHttp("https://api.example.com/sync");
});
```

## MAUI/WinUI com SQLite

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
    cfg.OnRemoteChangeReceived(async change =>
    {
        // Atualizar UI em tempo real
        await InvokeAsync(StateHasChanged);
    });
});
```

## Integração com XForge.MediatR

```csharp
public class SyncCommandHandler : IRequestHandler<SyncCommand, SyncResult>
{
    private readonly ISyncEngine _engine;

    public async ValueTask<SyncResult> Handle(SyncCommand request, CancellationToken ct)
    {
        return await _engine.SyncAsync(ct);
    }
}
```

---

<div align="center">

**Próximo:** [Testing →](testing.md)

</div>
