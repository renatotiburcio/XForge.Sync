# 15 — Performance

## Delta Sync vs Full Sync

| Operation | Delta Sync | Full Sync |
|-----------|-----------|-----------|
| 100 changed entities | ~10KB | ~1MB |
| 1000 changed entities | ~100KB | ~10MB |

## Batch Size

```csharp
cfg.BatchSize = 10;  // Slow network
cfg.BatchSize = 500; // Fast network
```

## Metrics

| Operation | Typical Latency |
|-----------|----------------|
| Track local | < 1ms |
| Get pending (100 items) | < 5ms |
| Sync via HTTP (100 items) | < 500ms |

---

<div align="center">

**Next:** [Troubleshooting →](troubleshooting.md)

</div>
