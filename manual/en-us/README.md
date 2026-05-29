# XForge.Sync — Official Manual

<p align="center">
  <img src="./icon.png" alt="XForge.Sync" width="128" height="128" />
</p>

<p align="center">
  <strong>Offline-first sync engine for .NET</strong>
</p>

<p align="center">
  <img src="https://img.shields.io/nuget/v/XForge.Sync.svg" alt="NuGet" />
  <img src="https://img.shields.io/badge/version-0.4.0-blue" alt="Version" />
  <img src="https://img.shields.io/badge/status-Published-green" alt="Status" />
  <img src="https://img.shields.io/badge/license-MIT-blue" alt="License" />
  <img src="https://img.shields.io/badge/.NET-8.0%20%7C%209.0%20%7C%2010.0-purple" alt=".NET" />
</p>

<p align="center">
  <a href="https://www.nuget.org/packages/XForge.Sync/">NuGet</a> ·
  <a href="https://github.com/renatotiburcio/XForge.Sync">GitHub</a>
</p>

---

> ✅ **Release:** v0.4.0 — APIs follow Semantic Versioning.

---

## Table of Contents

| # | Chapter | File |
|---|---------|------|
| 01–04 | Cover, Introduction, Status, Features | [README.md](README.md) |
| 05 | Installation | [installation.md](installation.md) |
| 06 | Quick Start | [quick-start.md](quick-start.md) |
| 07 | Configuration | [configuration.md](configuration.md) |
| 08 | Architecture | [architecture.md](architecture.md) |
| 09 | Basic Usage | [basic-usage.md](basic-usage.md) |
| 10 | Intermediate Usage | [intermediate-usage.md](intermediate-usage.md) |
| 11 | Advanced Usage | [advanced-usage.md](advanced-usage.md) |
| 12 | Enterprise Best Practices | [enterprise-best-practices.md](enterprise-best-practices.md) |
| 13 | Integration Examples | [integration-examples.md](integration-examples.md) |
| 14 | Testing | [testing.md](testing.md) |
| 15 | Performance | [performance.md](performance.md) |
| 16 | Troubleshooting | [troubleshooting.md](troubleshooting.md) |
| 17 | FAQ | [faq.md](faq.md) |
| 18 | Roadmap | [roadmap.md](roadmap.md) |
| 19 | Changelog | [changelog.md](changelog.md) |
| 20 | API Reference | [api-reference.md](api-reference.md) |
| 21 | Competitor Comparison | [package-comparison.md](package-comparison.md) |
| 22 | Migration Guide | [migration-guide.md](migration-guide.md) |
| 23 | Contributing | [contributing.md](contributing.md) |
| 24 | License | [license.md](license.md) |
| 25 | Final Notes | [final-notes.md](final-notes.md) |
| 26 | Extensibility | [extensibility.md](extensibility.md) |
| 27 | Offline-First Patterns | [offline-sync-patterns.md](offline-sync-patterns.md) |
| 28 | Multi-TFM Compatibility | [multi-tfm-compatibility.md](multi-tfm-compatibility.md) |
| 29 | Security | [security.md](security.md) |
| 30 | Support & Community | [support-community.md](support-community.md) |

---

## 01 — Cover

| Field | Value |
|-------|-------|
| **Name** | XForge.Sync |
| **Version** | 0.4.0 |
| **Status** | Published |
| **Last Updated** | 2026-05-29 |
| **License** | MIT |
| **Repository** | [github.com/renatotiburcio/XForge.Sync](https://github.com/renatotiburcio/XForge.Sync) |

---

## 02 — Introduction

### What It Is

XForge.Sync is a .NET library for offline-first synchronization with multiple transport support. It provides ChangeTracker to track local changes and ConflictResolver to resolve synchronization conflicts, with transports for SQLite, HTTP, SignalR, and IndexedDB.

```csharp
var tracker = new ChangeTracker(localStore);
await tracker.TrackAsync(entity, ChangeType.Modified);
var result = await engine.SyncAsync();
```

### Why It Exists

Modern applications need to work offline and sync when connectivity returns. XForge.Sync addresses:

- **Change tracking** — automatically track local changes.
- **Conflict resolution** — resolve conflicts deterministically.
- **Delta sync** — transmit only changes, not complete data.
- **Multi-transport** — SQLite local, HTTP REST, SignalR real-time, IndexedDB (Blazor WASM).

### Design Philosophy

1. **Offline-first** — the application works without connectivity.
2. **Transport-agnostic** — sync logic is independent of transport.
3. **Conflict-aware** — conflicts are first-class citizens.
4. **Delta sync** — only changes are transmitted.

---

## 03 — Package Status

### Current Phase: Published

### Stable Features

| Feature | Description |
|---------|-------------|
| **ChangeTracker** | Change tracking (Create, Update, Delete) |
| **ConflictResolver** | Conflict resolution (Last-Write-Wins, Manual, Custom) |
| **SQLite Transport** | Local storage with SQLite |
| **HTTP Transport** | Sync via REST API |
| **SignalR Transport** | Real-time sync |
| **IndexedDB Transport** | Storage for Blazor WASM |
| **Delta Sync** | Transmit only changes |

---

## 04 — Features

### 4.1 — ChangeTracker

Tracks entity changes for later synchronization.

### 4.2 — ConflictResolver

Resolves conflicts between local and remote versions.

### 4.3 — Delta Sync

Transmits only differences (deltas), reducing bandwidth.

### 4.4–4.7 — Transports

SQLite, HTTP, SignalR, IndexedDB.

### Packages

| Package | Description |
|---------|-------------|
| `XForge.Sync` | Core engine |
| `XForge.Sync.Sqlite` | SQLite transport |
| `XForge.Sync.Http` | HTTP/REST transport |
| `XForge.Sync.SignalR` | SignalR transport |
| `XForge.Sync.IndexedDB` | IndexedDB transport (Blazor WASM) |
| `XForge.Sync.AspNetCore` | ASP.NET Core integration |

---

<div align="center">

**Next:** [Installation →](installation.md)

</div>
