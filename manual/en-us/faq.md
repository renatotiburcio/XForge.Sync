# 17 — FAQ

## Does XForge.Sync work offline?

Yes. All change tracking logic works offline. Synchronization occurs when connectivity is available.

## Can I use it with Entity Framework Core?

Yes. The ChangeTracker can be integrated with EF Core using interceptors.

## How does conflict resolution work?

Three strategies: Last-Write-Wins (automatic), Manual (callback), Custom (IConflictResolver).

## Does it support real-time sync?

Yes, via SignalR transport. Remote changes are received automatically.

---

<div align="center">

**Next:** [Roadmap →](roadmap.md)

</div>
