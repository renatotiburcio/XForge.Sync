# 16 — Troubleshooting

## Mudanças não estão sendo sincronizadas

**Causa:** O ChangeTracker não foi inicializado corretamente.

**Solução:** Certifique-se de chamar `InitializeAsync()` no startup.

## Conflitos frequentes

**Causa:** Múltiplos usuários editando a mesma entidade simultaneamente.

**Solução:** Use uma estratégia de conflito adequada (Last-Write-Wins, Manual, Merge).

## Sync lento com muitas entidades

**Causa:** Batch size muito grande ou rede lenta.

**Solução:** Reduza `BatchSize` para 10-50.

## Erro de timeout no HTTP transport

**Causa:** Servidor não responde dentro do timeout.

**Solução:** Aumente o timeout ou configure retry.

```csharp
cfg.UseHttp("https://api.example.com/sync", options =>
{
    options.Timeout = TimeSpan.FromSeconds(30);
});
```

---

<div align="center">

**Próximo:** [FAQ →](faq.md)

</div>
