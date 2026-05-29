# XForge.Sync — Manual Oficial

<p align="center">
  <img src="./icon.png" alt="XForge.Sync" width="128" height="128" />
</p>

<p align="center">
  <strong>Engine de sincronização offline-first para .NET</strong>
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

> ✅ **Release:** v0.4.0 — APIs seguem Semantic Versioning.

---

## Sumário

| # | Capítulo | Arquivo |
|---|----------|---------|
| 01–04 | Capa, Introdução, Status, Features | [README.md](README.md) |
| 05 | Instalação | [installation.md](installation.md) |
| 06 | Quick Start | [quick-start.md](quick-start.md) |
| 07 | Configuração | [configuration.md](configuration.md) |
| 08 | Arquitetura | [architecture.md](architecture.md) |
| 09 | Uso Básico | [basic-usage.md](basic-usage.md) |
| 10 | Uso Intermediário | [intermediate-usage.md](intermediate-usage.md) |
| 11 | Uso Avançado | [advanced-usage.md](advanced-usage.md) |
| 12 | Boas Práticas Enterprise | [enterprise-best-practices.md](enterprise-best-practices.md) |
| 13 | Exemplos de Integração | [integration-examples.md](integration-examples.md) |
| 14 | Testing | [testing.md](testing.md) |
| 15 | Performance | [performance.md](performance.md) |
| 16 | Troubleshooting | [troubleshooting.md](troubleshooting.md) |
| 17 | FAQ | [faq.md](faq.md) |
| 18 | Roadmap | [roadmap.md](roadmap.md) |
| 19 | Changelog | [changelog.md](changelog.md) |
| 20 | Referência da API | [api-reference.md](api-reference.md) |
| 21 | Comparação com Concorrentes | [package-comparison.md](package-comparison.md) |
| 22 | Guia de Migração | [migration-guide.md](migration-guide.md) |
| 23 | Contribuindo | [contributing.md](contributing.md) |
| 24 | Licença | [license.md](license.md) |
| 25 | Notas Finais | [final-notes.md](final-notes.md) |
| 26 | Extensibilidade | [extensibility.md](extensibility.md) |
| 27 | Padrões Offline-First | [offline-sync-patterns.md](offline-sync-patterns.md) |
| 28 | Compatibilidade Multi-TFM | [multi-tfm-compatibility.md](multi-tfm-compatibility.md) |
| 29 | Segurança | [security.md](security.md) |
| 30 | Suporte e Comunidade | [support-community.md](support-community.md) |

---

## 01 — Capa

| Campo | Valor |
|-------|-------|
| **Nome** | XForge.Sync |
| **Versão** | 0.4.0 |
| **Status** | Published |
| **Última atualização** | 2026-05-29 |
| **Licença** | MIT |
| **Repositório** | [github.com/renatotiburcio/XForge.Sync](https://github.com/renatotiburcio/XForge.Sync) |

---

## 02 — Introdução

### O que é

XForge.Sync é uma biblioteca .NET para sincronização offline-first com suporte a múltiplos transportes. Ela fornece `ChangeTracker` para rastrear alterações locais e `ConflictResolver` para resolver conflitos de sincronização, com transportes para SQLite, HTTP, SignalR e IndexedDB.

```csharp
var tracker = new ChangeTracker(localStore);
await tracker.TrackAsync(entity, ChangeType.Modified);
var result = await syncEngine.SyncAsync(cancellationToken);
```

### Por que existe

Aplicações modernas precisam funcionar offline e sincronizar quando a conexão retorna. O XForge.Sync resolve:

- **Change tracking** — rastrear alterações locais automaticamente.
- **Conflict resolution** — resolver conflitos de forma determinística.
- **Delta sync** — transmitir apenas mudanças, não dados completos.
- **Multi-transporte** — SQLite local, HTTP REST, SignalR real-time, IndexedDB (Blazor WASM).

### Filosofia de Design

1. **Offline-first** — a aplicação funciona sem conexão.
2. **Transport-agnostic** — a lógica de sync é independente do transporte.
3. **Conflict-aware** — conflitos são cidadãos de primeira classe.
4. **Delta sync** — apenas mudanças são transmitidas.

---

## 03 — Status do Pacote

### Fase Atual: Published

### Recursos Estáveis

| Recurso | Descrição |
|---------|-----------|
| **ChangeTracker** | Rastreamento de alterações (Create, Update, Delete) |
| **ConflictResolver** | Resolução de conflitos (Last-Write-Wins, Manual, Custom) |
| **SQLite Transport** | Armazenamento local com SQLite |
| **HTTP Transport** | Sincronização via REST API |
| **SignalR Transport** | Sincronização em tempo real |
| **IndexedDB Transport** | Armazenamento para Blazor WASM |
| **Delta Sync** | Transmissão apenas de mudanças |

### Matriz de Compatibilidade

| Target Framework | Status |
|------------------|--------|
| `net8.0` | ✅ Suportado |
| `net9.0` | ✅ Suportado |
| `net10.0` | ✅ Suportado |

---

## 04 — Features

### 4.1 — ChangeTracker

Rastreia alterações em entidades para sincronização posterior.

```csharp
var tracker = new ChangeTracker(store);
await tracker.TrackAsync(order, ChangeType.Modified);
var pending = await tracker.GetPendingChangesAsync();
```

### 4.2 — ConflictResolver

Resolve conflitos entre versões local e remota.

```csharp
var resolver = new LastWriteWinsResolver();
var resolved = resolver.Resolve(localChange, remoteChange);
```

### 4.3 — Delta Sync

Transmite apenas as mudanças (deltas), reduzindo bandwidth.

### 4.4 — SQLite Transport

Armazenamento local persistente com SQLite.

### 4.5 — HTTP Transport

Sincronização via REST API com retry automático.

### 4.6 — SignalR Transport

Sincronização em tempo real via WebSocket.

### 4.7 — IndexedDB Transport

Armazenamento para Blazor WebAssembly via IndexedDB.

### Pacotes

| Pacote | Descrição |
|--------|-----------|
| `XForge.Sync` | Core engine: ChangeTracker, ConflictResolver |
| `XForge.Sync.Sqlite` | Transporte SQLite |
| `XForge.Sync.Http` | Transporte HTTP/REST |
| `XForge.Sync.SignalR` | Transporte SignalR |
| `XForge.Sync.IndexedDB` | Transporte IndexedDB (Blazor WASM) |
| `XForge.Sync.AspNetCore` | Integração ASP.NET Core |

---

<div align="center">

**Próximo:** [Instalação →](installation.md)

</div>
