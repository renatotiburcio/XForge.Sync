# 27 — Padrões Offline-First

## Padrão de Queue Local

Todas as alterações são enfileiradas localmente antes de sincronizar:

```csharp
await tracker.TrackAsync(entity, changeType);
// A alteração persiste localmente até sync
```

## Padrão de Sync Automático

```csharp
// Sync ao reconectar
connectivity.ConnectivityChanged += async (s, e) =>
{
    if (e.IsConnected)
        await engine.SyncAsync();
};
```

## Padrão de Resolução por Prioridade

```csharp
public class PriorityResolver : IConflictResolver
{
    public ResolvedChange Resolve(Change local, Change remote)
    {
        // Dados do servidor têm prioridade para status de pagamento
        if (remote.Delta.ContainsKey("PaymentStatus"))
            return new ResolvedChange(remote);
        // Dados locais têm prioridade para carrinho
        return new ResolvedChange(local);
    }
}
```

## Padrão de Sync em Background

```csharp
// Timer de sync periódico
var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));
while (await timer.WaitForNextTickAsync())
{
    if (await connectivity.IsConnectedAsync())
        await engine.SyncAsync();
}
```

---

<div align="center">

**Próximo:** [Compatibilidade Multi-TFM →](multi-tfm-compatibility.md)

</div>
