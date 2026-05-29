namespace XForge.Sync.Tests;

public class DefaultSyncPolicyTests
{
    private readonly DefaultSyncPolicy _policy = new();

    [Fact]
    public void ShouldSync_ReturnsTrue_WhenOnlineAndHasPendingChanges()
    {
        // Arrange
        SyncContext context = new()
        {
            IsOnline = true,
            PendingChanges = 5,
            TimeSinceLastSync = TimeSpan.FromMinutes(1)
        };

        // Act
        bool result = _policy.ShouldSync(context);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ShouldSync_ReturnsFalse_WhenOffline()
    {
        // Arrange
        SyncContext context = new()
        {
            IsOnline = false,
            PendingChanges = 5,
            TimeSinceLastSync = TimeSpan.FromHours(1)
        };

        // Act
        bool result = _policy.ShouldSync(context);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ShouldSync_ReturnsTrue_WhenOnlineAndIntervalElapsed()
    {
        // Arrange
        SyncContext context = new()
        {
            IsOnline = true,
            PendingChanges = 0,
            TimeSinceLastSync = TimeSpan.FromMinutes(10) // > 5 min interval
        };

        // Act
        bool result = _policy.ShouldSync(context);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ShouldSync_ReturnsFalse_WhenOnlineNoPendingAndIntervalNotElapsed()
    {
        // Arrange
        SyncContext context = new()
        {
            IsOnline = true,
            PendingChanges = 0,
            TimeSinceLastSync = TimeSpan.FromMinutes(1)
        };

        // Act
        bool result = _policy.ShouldSync(context);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetSyncInterval_ReturnsFiveMinutes()
    {
        // Act
        TimeSpan interval = _policy.GetSyncInterval();

        // Assert
        interval.Should().Be(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void GetMaxRetries_ReturnsThree()
    {
        // Act
        int maxRetries = _policy.GetMaxRetries();

        // Assert
        maxRetries.Should().Be(3);
    }

    [Fact]
    public void ShouldSync_ThrowsOnNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _policy.ShouldSync(null!));
    }
}
