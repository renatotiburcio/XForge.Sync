# 26 — Extensibilidade

## Custom Transport

Implemente `ISyncTransport` para qualquer protocolo:

```csharp
public class WebSocketTransport : ISyncTransport
{
    public async Task<SyncResponse> SendAsync(SyncRequest request, CancellationToken ct)
    {
        // Implementar via WebSocket
    }
}
```

## Custom Conflict Resolver

```csharp
public class BusinessRuleResolver : IConflictResolver
{
    public ResolvedChange Resolve(Change local, Change remote)
    {
        // Regra de negócio customizada
    }
}
```

## Custom Change Store

Implemente `IChangeStore` para qualquer banco de dados:

```csharp
public class PostgresChangeStore : IChangeStore { ... }
```

---

<div align="center">

**Próximo:** [Padrões Offline-First →](offline-sync-patterns.md)

</div>
