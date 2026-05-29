# 09 — Uso Básico

## Rastrear Alterações

```csharp
// Criar
await tracker.TrackAsync(newOrder, ChangeType.Created);

// Atualizar
await tracker.TrackAsync(updatedOrder, ChangeType.Modified);

// Deletar
await tracker.TrackAsync(deletedOrder, ChangeType.Deleted);
```

## Obter Mudanças Pendentes

```csharp
var pending = await tracker.GetPendingChangesAsync();
foreach (var change in pending)
{
    Console.WriteLine($"{change.EntityType} - {change.ChangeType} - {change.Timestamp}");
}
```

## Sincronizar

```csharp
var result = await engine.SyncAsync();

foreach (var conflict in result.Conflicts)
{
    Console.WriteLine($"Conflito: {conflict.EntityId} - Local: {conflict.LocalTimestamp}, Remoto: {conflict.RemoteTimestamp}");
}
```

## Resolver Conflitos

```csharp
// Last-Write-Wins (automático)
var resolver = new LastWriteWinsResolver();

// Manual
var resolver = new ManualResolver((local, remote) =>
{
    return local.Timestamp > remote.Timestamp ? local : remote;
});
```

---

<div align="center">

**Próximo:** [Uso Intermediário →](intermediate-usage.md)

</div>
