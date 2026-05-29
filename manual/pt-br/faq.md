# 17 — FAQ

## O XForge.Sync funciona offline?

Sim. Toda a lógica de rastreamento de mudanças funciona offline. A sincronização ocorre quando a conexão está disponível.

## Posso usar com Entity Framework Core?

Sim. O ChangeTracker pode ser integrado com EF Core usando interceptors.

## Como funciona a resolução de conflitos?

O XForge.Sync suporta três estratégias: Last-Write-Wins (automático), Manual (callback), e Custom (implementação de IConflictResolver).

## Suporta sincronização em tempo real?

Sim, via SignalR transport. Mudanças remotas são recebidas automaticamente.

## Qual a diferença entre Push e Sync?

Push envia apenas mudanças locais. Sync faz push + pull bidirecional.

---

<div align="center">

**Próximo:** [Roadmap →](roadmap.md)

</div>
