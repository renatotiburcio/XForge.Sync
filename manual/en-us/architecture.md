# 08 — Architecture

## Overview

```
┌──────────────────────────────┐
│       Application            │
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

## Components

| Component | Responsibility |
|-----------|---------------|
| `ChangeTracker` | Track local changes |
| `ConflictResolver` | Resolve sync conflicts |
| `SyncEngine` | Orchestrate sync process |
| `IChangeStore` | Local change storage |
| `ISyncTransport` | Data transport |

---

<div align="center">

**Next:** [Basic Usage →](basic-usage.md)

</div>
