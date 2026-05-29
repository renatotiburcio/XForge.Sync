# 16 — Troubleshooting

## Changes not syncing

**Cause:** ChangeTracker not initialized. **Solution:** Call `InitializeAsync()` at startup.

## Frequent conflicts

**Cause:** Multiple users editing same entity. **Solution:** Use appropriate conflict strategy.

## Slow sync

**Cause:** Large batch size or slow network. **Solution:** Reduce `BatchSize` to 10-50.

## HTTP timeout

**Cause:** Server not responding. **Solution:** Increase timeout or configure retry.

---

<div align="center">

**Next:** [FAQ →](faq.md)

</div>
