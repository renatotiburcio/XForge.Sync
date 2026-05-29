# 05 — Installation

## Prerequisites

| Requirement | Minimum Version | Notes |
|-------------|----------------|-------|
| .NET SDK | 8.0+ | LTS recommended |
| C# | 12+ | Enabled by default in .NET 8+ |
| IDE | VS 2022 17.8+, Rider 2023.3+, VS Code | Any is sufficient |

## Installation via .NET CLI

```bash
dotnet add package XForge.Sync
```

Optional transports:

```bash
dotnet add package XForge.Sync.Sqlite
dotnet add package XForge.Sync.Http
dotnet add package XForge.Sync.SignalR
dotnet add package XForge.Sync.IndexedDB
dotnet add package XForge.Sync.AspNetCore
```

---

<div align="center">

**Next:** [Quick Start →](quick-start.md)

</div>
