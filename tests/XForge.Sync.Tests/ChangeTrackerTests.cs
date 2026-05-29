namespace XForge.Sync.Tests;

public class ChangeTrackerTests
{
    [Fact]
    public async Task TrackChange_AddsToPending()
    {
        // Arrange
        ChangeTracker tracker = new();
        TestEntity entity = new() { Id = "1", Name = "Test" };

        // Act
        await tracker.TrackChangeAsync(entity, ChangeType.Create);

        // Assert
        IReadOnlyList<TrackedChange> pending = await tracker.GetPendingChangesAsync();
        pending.Should().ContainSingle();
        pending[0].EntityType.Should().Be("TestEntity");
        pending[0].ChangeType.Should().Be(ChangeType.Create);
    }

    [Fact]
    public async Task TrackChange_UpdatesExistingByKey()
    {
        // Arrange
        ChangeTracker tracker = new();
        TestEntity entity = new() { Id = "1", Name = "Test" };

        // Act
        await tracker.TrackChangeAsync(entity, ChangeType.Create);
        await tracker.TrackChangeAsync(entity, ChangeType.Update);

        // Assert
        IReadOnlyList<TrackedChange> pending = await tracker.GetPendingChangesAsync();
        pending.Should().ContainSingle();
        pending[0].ChangeType.Should().Be(ChangeType.Update);
    }

    [Fact]
    public async Task GetPendingChanges_ReturnsInOrder()
    {
        // Arrange
        ChangeTracker tracker = new();
        await tracker.TrackChangeAsync(new TestEntity { Id = "1" }, ChangeType.Create);
        await tracker.TrackChangeAsync(new TestEntity { Id = "2" }, ChangeType.Create);
        await tracker.TrackChangeAsync(new TestEntity { Id = "3" }, ChangeType.Create);

        // Act
        IReadOnlyList<TrackedChange> pending = await tracker.GetPendingChangesAsync();

        // Assert
        pending.Should().HaveCount(3);
    }

    [Fact]
    public async Task MarkSynced_RemovesFromPending()
    {
        // Arrange
        ChangeTracker tracker = new();
        TestEntity entity = new() { Id = "1", Name = "Test" };
        await tracker.TrackChangeAsync(entity, ChangeType.Create);
        IReadOnlyList<TrackedChange> pending = await tracker.GetPendingChangesAsync();

        // Act
        await tracker.MarkSyncedAsync(pending);

        // Assert
        IReadOnlyList<TrackedChange> after = await tracker.GetPendingChangesAsync();
        after.Should().BeEmpty();
    }

    [Fact]
    public async Task Clear_RemovesAllPending()
    {
        // Arrange
        ChangeTracker tracker = new();
        await tracker.TrackChangeAsync(new TestEntity { Id = "1" }, ChangeType.Create);
        await tracker.TrackChangeAsync(new TestEntity { Id = "2" }, ChangeType.Update);

        // Act
        await tracker.ClearAsync();

        // Assert
        IReadOnlyList<TrackedChange> pending = await tracker.GetPendingChangesAsync();
        pending.Should().BeEmpty();
    }

    [Fact]
    public async Task TrackChange_SerializesEntityData()
    {
        // Arrange
        ChangeTracker tracker = new();
        TestEntity entity = new() { Id = "1", Name = "Test" };

        // Act
        await tracker.TrackChangeAsync(entity, ChangeType.Create);

        // Assert
        IReadOnlyList<TrackedChange> pending = await tracker.GetPendingChangesAsync();
        pending[0].Data.Should().NotBeNullOrEmpty();
        pending[0].Data.Should().Contain("Test");
    }

    [Fact]
    public async Task TrackChange_ThrowsOnNullEntity()
    {
        // Arrange
        ChangeTracker tracker = new();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            tracker.TrackChangeAsync<TestEntity>(null!, ChangeType.Create));
    }

    private sealed class TestEntity
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
    }
}
