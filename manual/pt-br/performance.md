# 15 — Performance

## Delta Sync vs Full Sync

| Operação | Delta Sync | Full Sync |
|----------|-----------|-----------|
| 100 entidades alteradas | ~10KB | ~1MB |
| 1000 entidades alteradas | ~100KB | ~10MB |

## Batch Size

Ajuste o `BatchSize` conforme a rede:

```csharp
// Rede lenta
cfg.BatchSize = 10;

// Rede rápida
cfg.BatchSize = 500;
```

## SQLite Performance

```csharp
// Use WAL mode para melhor concorrência
cfg.UseSqlite("./sync.db", options =>
{
    options.JournalMode = SqliteJournalMode.Wal;
    options.CacheSize = 10000;
});
```

## Métricas

| Operação | Latência típica |
|----------|----------------|
| Track local | < 1ms |
| Get pending (100 items) | < 5ms |
| Sync via HTTP (100 items) | < 500ms |

---

<div align="center">

**Próximo:** [Troubleshooting →](troubleshooting.md)

</div>
