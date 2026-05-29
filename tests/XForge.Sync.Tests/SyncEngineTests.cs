using Microsoft.Extensions.Logging;

namespace XForge.Sync.Tests;

public class SyncEngineTests
{
    private readonly Mock<ISyncQueue> _queueMock = new();
    private readonly Mock<ISyncTransport> _transportMock = new();
    private readonly Mock<IConflictResolver> _resolverMock = new();
    private readonly Mock<IChangeTracker> _trackerMock = new();
    private readonly Mock<ISyncPolicy> _policyMock = new();
    private readonly ConnectivityMonitor _connectivity = new();
    private readonly Mock<ILogger<SyncEngine>> _loggerMock = new();

    private SyncEngine CreateEngine() =>
        new(
            _queueMock.Object,
            _transportMock.Object,
            _resolverMock.Object,
            _trackerMock.Object,
            _policyMock.Object,
            _connectivity,
            _loggerMock.Object);

    [Fact]
    public async Task GetStatusAsync_ReturnsIdle_Initially()
    {
        // Arrange
        SyncEngine engine = CreateEngine();

        // Act
        SyncStatus status = await engine.GetStatusAsync();

        // Assert
        status.Should().Be(SyncStatus.Idle);
    }

    [Fact]
    public async Task PushAsync_ReturnsFailure_WhenOffline()
    {
        // Arrange
        _connectivity.SetOnlineStatus(false);
        SyncEngine engine = CreateEngine();

        // Act
        SyncResult result = await engine.PushAsync();

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("offline");
    }

    [Fact]
    public async Task PullAsync_ReturnsFailure_WhenOffline()
    {
        // Arrange
        _connectivity.SetOnlineStatus(false);
        SyncEngine engine = CreateEngine();

        // Act
        SyncResult result = await engine.PullAsync();

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("offline");
    }

    [Fact]
    public async Task PushAsync_ReturnsSuccess_WhenNoPendingChanges()
    {
        // Arrange
        _trackerMock.Setup(t => t.GetPendingChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        SyncEngine engine = CreateEngine();

        // Act
        SyncResult result = await engine.PushAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.ItemsPushed.Should().Be(0);
    }

    [Fact]
    public async Task PushAsync_SendsChanges_AndMarksSynced()
    {
        // Arrange
        List<TrackedChange> changes =
        [
            new() { Key = "Entity:1", ChangeType = ChangeType.Create, EntityType = "Entity" }
        ];

        _trackerMock.Setup(t => t.GetPendingChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(changes);
        _transportMock.Setup(t => t.SendAsync(It.IsAny<SyncRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SyncResponse { IsSuccess = true });

        SyncEngine engine = CreateEngine();

        // Act
        SyncResult result = await engine.PushAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.ItemsPushed.Should().Be(1);
        _trackerMock.Verify(t => t.MarkSyncedAsync(changes, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PushAsync_ReturnsFailure_WhenTransportFails()
    {
        // Arrange
        _trackerMock.Setup(t => t.GetPendingChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new() { Key = "1", ChangeType = ChangeType.Create, EntityType = "E" }]);
        _transportMock.Setup(t => t.SendAsync(It.IsAny<SyncRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SyncResponse { IsSuccess = false, ErrorMessage = "Server error" });

        SyncEngine engine = CreateEngine();

        // Act
        SyncResult result = await engine.PushAsync();

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Server error");
    }

    [Fact]
    public async Task SyncAsync_ReturnsFailure_WhenPolicyDeclines()
    {
        // Arrange
        _policyMock.Setup(p => p.ShouldSync(It.IsAny<SyncContext>())).Returns(false);
        _queueMock.Setup(q => q.GetPendingCountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);
        SyncEngine engine = CreateEngine();

        // Act
        SyncResult result = await engine.SyncAsync();

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("policy declined");
    }

    [Fact]
    public async Task SyncAsync_PerformsPushThenPull()
    {
        // Arrange
        List<TrackedChange> changes =
        [
            new() { Key = "Entity:1", ChangeType = ChangeType.Create, EntityType = "Entity" }
        ];

        _policyMock.Setup(p => p.ShouldSync(It.IsAny<SyncContext>())).Returns(true);
        _queueMock.Setup(q => q.GetPendingCountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _trackerMock.Setup(t => t.GetPendingChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(changes);
        _transportMock.Setup(t => t.SendAsync(It.IsAny<SyncRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SyncResponse { IsSuccess = true, Changes = [] });

        SyncEngine engine = CreateEngine();

        // Act
        SyncResult result = await engine.SyncAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        // Push sends 1 request, Pull sends 1 request = at least 2
        _transportMock.Verify(
            t => t.SendAsync(It.IsAny<SyncRequest>(), It.IsAny<CancellationToken>()),
            Times.AtLeast(2));
    }

    [Fact]
    public async Task SyncAsync_ReturnsFailure_WhenOffline()
    {
        // Arrange
        _connectivity.SetOnlineStatus(false);
        SyncEngine engine = CreateEngine();

        // Act
        SyncResult result = await engine.SyncAsync();

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("offline");
    }
}
