# 08 — Arquitetura

## Visão Geral

```
┌──────────────────────────────┐
│       Aplicação              │
└──────────┬───────────────────┘
           │
           ▼
┌──────────────────────────────┐
│   XForge.Sync (Core)         │
│   ┌────────────────────┐     │
│   │ ChangeTracker      │     │
│   │ ConflictResolver   │     │
│   │ SyncEngine         │     │
│   └────────────────────┘     │
└──────────┬───────────────────┘
           │
    ┌──────┼──────┬──────────┐
    ▼      ▼      ▼          ▼
 SQLite  HTTP  SignalR  IndexedDB
```

## Componentes

| Componente | Responsabilidade |
|-----------|-----------------|
| `ChangeTracker` | Rastrear alterações locais |
| `ConflictResolver` | Resolver conflitos de sync |
| `SyncEngine` | Orquestrar processo de sincronização |
| `IChangeStore` | Armazenamento local de mudanças |
| `ISyncTransport` | Transporte de dados (HTTP, SignalR, etc.) |

---

<div align="center">

**Próximo:** [Uso Básico →](basic-usage.md)

</div>
