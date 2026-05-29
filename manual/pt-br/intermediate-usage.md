# 10 — Uso Intermediário

## Delta Sync

O XForge.Sync transmite apenas as diferenças (deltas):

```csharp
// O ChangeTracker detecta campos alterados
var change = await tracker.TrackAsync(order, ChangeType.Modified);
// change.Delta contém apenas os campos modificados
```

## Batch Sync

```csharp
var options = new SyncOptions { BatchSize = 50 };
var result = await engine.SyncAsync(options);
```

## Sync Bidirecional

```csharp
// Enviar mudanças locais
var uploadResult = await engine.PushAsync();

// Receber mudanças remotas
var downloadResult = await engine.PullAsync();

// Ou ambos
var syncResult = await engine.SyncAsync();
```

## Filtragem de Entidades

```csharp
// Sync apenas de pedidos
var result = await engine.SyncAsync(filter: change =>
    change.EntityType == "Order" && change.Timestamp > lastSync);
```

---

<div align="center">

**Próximo:** [Uso Avançado →](advanced-usage.md)

</div>
