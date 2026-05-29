# 05 — Instalação

## Pré-requisitos

| Requisito | Versão mínima | Observação |
|-----------|--------------|------------|
| .NET SDK | 8.0+ | LTS recomendado |
| C# | 12+ | Habilitado por padrão no .NET 8+ |
| IDE | VS 2022 17.8+, Rider 2023.3+, VS Code | Qualquer uma é suficiente |

## Instalação via .NET CLI

```bash
dotnet add package XForge.Sync
```

Transportes opcionais:

```bash
dotnet add package XForge.Sync.Sqlite
dotnet add package XForge.Sync.Http
dotnet add package XForge.Sync.SignalR
dotnet add package XForge.Sync.IndexedDB
dotnet add package XForge.Sync.AspNetCore
```

## PackageReference em .csproj

```xml
<ItemGroup>
  <PackageReference Include="XForge.Sync" Version="0.4.*" />
  <PackageReference Include="XForge.Sync.Sqlite" Version="0.4.*" />
</ItemGroup>
```

---

<div align="center">

**Próximo:** [Quick Start →](quick-start.md)

</div>
