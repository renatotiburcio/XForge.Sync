using Microsoft.Data.Sqlite;

namespace XForge.Sync.Sqlite.Tests;

/// <summary>
/// Tests for <see cref="SqliteChangeTracker"/>.
/// </summary>
public sealed class SqliteChangeTrackerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly SqliteChangeTracker _tracker;

    public SqliteChangeTrackerTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        SqliteSyncOptions options = new()
        {
            ConnectionString = "DataSource=:memory:",
            AutoCreateTables = true
        };

        _tracker = new SqliteChangeTracker(
            _connection,
            Microsoft.Extensions.Options.Options.Create(options),
            new Moq.Mock<Microsoft.Extensions.Logging.ILogger<SqliteChangeTracker>>().Object);
    }

    // -------------------------------------------------------------------
    // TrackChangeAsync / GetPendingChangesAsync
    // -------------------------------------------------------------------

    [Fact]
    public async Task TrackChangeAsync_Create_CreatesPendingChange()
    {
        TestEntity entity = new() { Id = 1, Name = "New Item" };

        await _tracker.TrackChangeAsync(entity, ChangeType.Create);
        IReadOnlyList<TrackedChange> changes = await _tracker.GetPendingChangesAsync();

        changes.Should().HaveCount(1);
        changes[0].EntityType.Should().Be("TestEntity");
        changes[0].ChangeType.Should().Be(ChangeType.Create);
        changes[0].Key.Should().Contain("TestEntity:");
        changes[0].Data.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task TrackChangeAsync_Update_RecordsUpdateChange()
    {
        TestEntity entity = new() { Id = 2, Name = "Updated" };

        await _tracker.TrackChangeAsync(entity, ChangeType.Update);
        IReadOnlyList<TrackedChange> changes = await _tracker.GetPendingChangesAsync();

        changes.Should().HaveCount(1);
        changes[0].ChangeType.Should().Be(ChangeType.Update);
    }

    [Fact]
    public async Task TrackChangeAsync_Delete_RecordsDeleteChange()
    {
        TestEntity entity = new() { Id = 3, Name = "Deleted" };

        await _tracker.TrackChangeAsync(entity, ChangeType.Delete);
        IReadOnlyList<TrackedChange> changes = await _tracker.GetPendingChangesAsync();

        changes.Should().HaveCount(1);
        changes[0].ChangeType.Should().Be(ChangeType.Delete);
    }

    [Fact]
    public async Task TrackChangeAsync_MultipleChanges_ReturnsAll()
    {
        await _tracker.TrackChangeAsync(new TestEntity { Id = 1, Name = "A" }, ChangeType.Create);
        await _tracker.TrackChangeAsync(new TestEntity { Id = 2, Name = "B" }, ChangeType.Update);
        await _tracker.TrackChangeAsync(new TestEntity { Id = 3, Name = "C" }, ChangeType.Delete);

        IReadOnlyList<TrackedChange> changes = await _tracker.GetPendingChangesAsync();

        changes.Should().HaveCount(3);
    }

    [Fact]
    public async Task TrackChangeAsync_SerializesEntityData()
    {
        TestEntity entity = new() { Id = 42, Name = "Serializable" };

        await _tracker.TrackChangeAsync(entity, ChangeType.Create);
        IReadOnlyList<TrackedChange> changes = await _tracker.GetPendingChangesAsync();

        changes[0].Data.Should().NotBeNullOrEmpty();
        changes[0].Data.Should().Contain("Serializable");
        changes[0].Data.Should().Contain("42");
    }

    [Fact]
    public async Task GetPendingChangesAsync_Empty_ReturnsEmptyList()
    {
        IReadOnlyList<TrackedChange> changes = await _tracker.GetPendingChangesAsync();

        changes.Should().BeEmpty();
    }

    // -------------------------------------------------------------------
    // MarkSyncedAsync
    // -------------------------------------------------------------------

    [Fact]
    public async Task MarkSyncedAsync_RemovesFromPending()
    {
        await _tracker.TrackChangeAsync(new TestEntity { Id = 1, Name = "A" }, ChangeType.Create);
        await _tracker.TrackChangeAsync(new TestEntity { Id = 2, Name = "B" }, ChangeType.Update);

        IReadOnlyList<TrackedChange> pending = await _tracker.GetPendingChangesAsync();

        await _tracker.MarkSyncedAsync([pending[0]]);
        IReadOnlyList<TrackedChange> remaining = await _tracker.GetPendingChangesAsync();

        remaining.Should().HaveCount(1);
        remaining[0].Key.Should().Be(pending[1].Key);
    }

    [Fact]
    public async Task MarkSyncedAsync_AllChanges_ReturnsEmpty()
    {
        await _tracker.TrackChangeAsync(new TestEntity { Id = 1, Name = "A" }, ChangeType.Create);

        IReadOnlyList<TrackedChange> pending = await _tracker.GetPendingChangesAsync();

        await _tracker.MarkSyncedAsync(pending);
        IReadOnlyList<TrackedChange> remaining = await _tracker.GetPendingChangesAsync();

        remaining.Should().BeEmpty();
    }

    [Fact]
    public async Task MarkSyncedAsync_NonMatchingVersion_DoesNotRemove()
    {
        await _tracker.TrackChangeAsync(new TestEntity { Id = 1, Name = "A" }, ChangeType.Create);

        IReadOnlyList<TrackedChange> pending = await _tracker.GetPendingChangesAsync();

        // Try to mark with wrong version
        TrackedChange wrongVersion = pending[0] with { Version = 999 };
        await _tracker.MarkSyncedAsync([wrongVersion]);

        IReadOnlyList<TrackedChange> remaining = await _tracker.GetPendingChangesAsync();
        remaining.Should().HaveCount(1); // Should still be pending
    }

    // -------------------------------------------------------------------
    // ClearAsync
    // -------------------------------------------------------------------

    [Fact]
    public async Task ClearAsync_RemovesAllChanges()
    {
        await _tracker.TrackChangeAsync(new TestEntity { Id = 1, Name = "A" }, ChangeType.Create);
        await _tracker.TrackChangeAsync(new TestEntity { Id = 2, Name = "B" }, ChangeType.Update);

        await _tracker.ClearAsync();
        IReadOnlyList<TrackedChange> remaining = await _tracker.GetPendingChangesAsync();

        remaining.Should().BeEmpty();
    }

    [Fact]
    public async Task ClearAsync_EmptyTracker_DoesNotThrow()
    {
        Func<Task> act = () => _tracker.ClearAsync();

        await act.Should().NotThrowAsync();
    }

    // -------------------------------------------------------------------
    // Argument validation
    // -------------------------------------------------------------------

    [Fact]
    public async Task TrackChangeAsync_NullEntity_Throws()
    {
        Func<Task> act = () => _tracker.TrackChangeAsync<TestEntity>(null!, ChangeType.Create);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task MarkSyncedAsync_NullChanges_Throws()
    {
        Func<Task> act = () => _tracker.MarkSyncedAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    // -------------------------------------------------------------------
    // Ordering
    // -------------------------------------------------------------------

    [Fact]
    public async Task GetPendingChangesAsync_ReturnsInTrackedAtOrder()
    {
        // Track changes with small delays to ensure ordering
        await _tracker.TrackChangeAsync(new TestEntity { Id = 1, Name = "First" }, ChangeType.Create);
        await Task.Delay(10);
        await _tracker.TrackChangeAsync(new TestEntity { Id = 2, Name = "Second" }, ChangeType.Create);
        await Task.Delay(10);
        await _tracker.TrackChangeAsync(new TestEntity { Id = 3, Name = "Third" }, ChangeType.Create);

        IReadOnlyList<TrackedChange> changes = await _tracker.GetPendingChangesAsync();

        changes.Should().HaveCount(3);
        changes[0].Data.Should().Contain("First");
        changes[1].Data.Should().Contain("Second");
        changes[2].Data.Should().Contain("Third");
    }

    // -------------------------------------------------------------------
    // Dispose
    // -------------------------------------------------------------------

    [Fact]
    public void Dispose_MultipleCalls_DoesNotThrow()
    {
        SqliteConnection conn = new("DataSource=:memory:");
        conn.Open();

        SqliteSyncOptions opts = new() { ConnectionString = "DataSource=:memory:" };
        SqliteChangeTracker tracker = new(
            conn,
            Microsoft.Extensions.Options.Options.Create(opts),
            new Moq.Mock<Microsoft.Extensions.Logging.ILogger<SqliteChangeTracker>>().Object);

        tracker.Dispose();

        // Second dispose should not throw
        Action act = tracker.Dispose;
        act.Should().NotThrow();

        conn.Dispose();
    }

    public void Dispose()
    {
        _tracker.Dispose();
        _connection.Dispose();
    }

    // -------------------------------------------------------------------
    // Test model
    // -------------------------------------------------------------------

    private sealed record TestEntity
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
    }
}
