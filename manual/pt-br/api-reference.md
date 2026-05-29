# 20 — Referência da API

## ChangeTracker

| Método | Retorno | Descrição |
|--------|---------|-----------|
| `TrackAsync(entity, changeType)` | `Task<Change>` | Rastrear alteração |
| `GetPendingChangesAsync()` | `Task<IReadOnlyList<Change>>` | Listar mudanças pendentes |
| `MarkSyncedAsync(changeIds)` | `Task` | Marcar como sincronizado |

## ConflictResolver

| Método | Retorno | Descrição |
|--------|---------|-----------|
| `Resolve(local, remote)` | `ResolvedChange` | Resolver conflito |

## SyncEngine

| Método | Retorno | Descrição |
|--------|---------|-----------|
| `SyncAsync(options, ct)` | `Task<SyncResult>` | Sincronizar (push + pull) |
| `PushAsync(ct)` | `Task<SyncResult>` | Enviar mudanças locais |
| `PullAsync(ct)` | `Task<SyncResult>` | Receber mudanças remotas |

## ISyncTransport

| Método | Retorno | Descrição |
|--------|---------|-----------|
| `SendAsync(request, ct)` | `Task<SyncResponse>` | Enviar dados |

## SyncResult

| Propriedade | Tipo | Descrição |
|-------------|------|-----------|
| `SyncedCount` | `int` | Entidades sincronizadas |
| `ConflictCount` | `int` | Conflitos encontrados |
| `Conflicts` | `IReadOnlyList<Conflict>` | Lista de conflitos |

---

<div align="center">

**Próximo:** [Comparação →](package-comparison.md)

</div>
