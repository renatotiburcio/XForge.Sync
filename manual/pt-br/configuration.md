# 07 — Configuração

## Configuração Básica

```csharp
builder.Services.AddXForgeSync(cfg =>
{
    cfg.UseSqlite("./local.db");
    cfg.UseHttp("https://api.example.com/sync");
    cfg.UseConflictResolver<LastWriteWinsResolver>();
});
```

## Opções do ChangeTracker

| Opção | Padrão | Descrição |
|-------|--------|-----------|
| `MaxRetries` | 3 | Tentativas de sync |
| `RetryDelay` | 1s | Delay entre tentativas |
| `BatchSize` | 100 | Itens por batch de sync |
| `ConflictStrategy` | LastWriteWins | Estratégia padrão de conflitos |

## Configuração por Ambiente

```json
{
  "Sync": {
    "Endpoint": "https://api.example.com/sync",
    "BatchSize": 100,
    "MaxRetries": 3,
    "ConflictStrategy": "LastWriteWins"
  }
}
```

---

<div align="center">

**Próximo:** [Arquitetura →](architecture.md)

</div>
