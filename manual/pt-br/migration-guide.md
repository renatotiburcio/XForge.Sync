# 22 — Guia de Migração

## De Sincronização Manual

### Antes

```csharp
var changes = db.Orders.Where(o => o.IsDirty);
foreach (var order in changes)
{
    await httpClient.PostAsJsonAsync("/api/sync", order);
    order.IsDirty = false;
    await db.SaveChangesAsync();
}
```

### Depois

```csharp
await tracker.TrackAsync(order, ChangeType.Modified);
var result = await engine.SyncAsync();
```

## De Firebase Realtime Database

```csharp
// Firebase
firebaseClient.Child("orders").AsObservable<Order>();

// XForge.Sync
cfg.UseSignalR("https://api.example.com/synchub");
cfg.OnRemoteChangeReceived(change => UpdateUI(change));
```

---

<div align="center">

**Próximo:** [Contribuindo →](contributing.md)

</div>
