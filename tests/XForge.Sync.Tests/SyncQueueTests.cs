namespace XForge.Sync.Tests;

public class SyncQueueTests
{
    [Fact]
    public async Task Enqueue_IncreasesPendingCount()
    {
        // Arrange
        SyncQueue queue = new();
        SyncOperation op = CreateOperation("1");

        // Act
        await queue.EnqueueAsync(op);

        // Assert
        int count = await queue.GetPendingCountAsync();
        count.Should().Be(1);
    }

    [Fact]
    public async Task Dequeue_ReturnsEnqueuedOperations()
    {
        // Arrange
        SyncQueue queue = new();
        await queue.EnqueueAsync(CreateOperation("1"));
        await queue.EnqueueAsync(CreateOperation("2"));

        // Act
        IReadOnlyList<SyncOperation> result = await queue.DequeueAsync(10);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task Dequeue_RespectsCountLimit()
    {
        // Arrange
        SyncQueue queue = new();
        await queue.EnqueueAsync(CreateOperation("1"));
        await queue.EnqueueAsync(CreateOperation("2"));
        await queue.EnqueueAsync(CreateOperation("3"));

        // Act
        IReadOnlyList<SyncOperation> result = await queue.DequeueAsync(2);

        // Assert
        result.Should().HaveCount(2);
        int remaining = await queue.GetPendingCountAsync();
        remaining.Should().Be(1);
    }

    [Fact]
    public async Task Dequeue_EmptyQueue_ReturnsEmpty()
    {
        // Arrange
        SyncQueue queue = new();

        // Act
        IReadOnlyList<SyncOperation> result = await queue.DequeueAsync(10);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Requeue_IncrementsRetryCount()
    {
        // Arrange
        SyncQueue queue = new();
        SyncOperation op = CreateOperation("1", retryCount: 2);

        // Act
        await queue.RequeueAsync(op);

        // Assert
        IReadOnlyList<SyncOperation> result = await queue.DequeueAsync(10);
        result.Should().ContainSingle();
        result[0].RetryCount.Should().Be(3);
    }

    [Fact]
    public async Task Enqueue_ThrowsOnNull()
    {
        // Arrange
        SyncQueue queue = new();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => queue.EnqueueAsync(null!));
    }

    [Fact]
    public async Task PendingCount_ReturnsZero_WhenEmpty()
    {
        // Arrange
        SyncQueue queue = new();

        // Act
        int count = await queue.GetPendingCountAsync();

        // Assert
        count.Should().Be(0);
    }

    private static SyncOperation CreateOperation(string id, int retryCount = 0) =>
        new()
        {
            OperationId = Guid.NewGuid().ToString(),
            Key = $"Entity:{id}",
            ChangeType = ChangeType.Create,
            EntityType = "Entity",
            Version = 1,
            RetryCount = retryCount,
            EnqueuedAt = DateTime.UtcNow
        };
}
