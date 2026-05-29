namespace XForge.Sync.Tests;

public class ConnectivityMonitorTests
{
    [Fact]
    public void IsOnline_DefaultsToTrue()
    {
        // Arrange & Act
        ConnectivityMonitor monitor = new();

        // Assert
        monitor.IsOnline.Should().BeTrue();
    }

    [Fact]
    public void SetOnlineStatus_ChangesIsOnline()
    {
        // Arrange
        ConnectivityMonitor monitor = new();

        // Act
        monitor.SetOnlineStatus(false);

        // Assert
        monitor.IsOnline.Should().BeFalse();
    }

    [Fact]
    public void SetOnlineStatus_RaisesEvent_WhenChanged()
    {
        // Arrange
        ConnectivityMonitor monitor = new();
        List<bool> events = [];
        monitor.ConnectivityChanged += events.Add;

        // Act
        monitor.SetOnlineStatus(false);

        // Assert
        events.Should().HaveCount(1).And.Contain(false);
    }

    [Fact]
    public void SetOnlineStatus_DoesNotRaiseEvent_WhenSameValue()
    {
        // Arrange
        ConnectivityMonitor monitor = new();
        int eventCount = 0;
        monitor.ConnectivityChanged += _ => eventCount++;

        // Act
        monitor.SetOnlineStatus(true); // same as default

        // Assert
        eventCount.Should().Be(0);
    }

    [Fact]
    public async Task CheckConnectivity_ReturnsCurrentStatus()
    {
        // Arrange
        ConnectivityMonitor monitor = new();
        monitor.SetOnlineStatus(false);

        // Act
        bool result = await monitor.CheckConnectivityAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void SetOnlineStatus_MultipleChanges_FiresEachTime()
    {
        // Arrange
        ConnectivityMonitor monitor = new();
        List<bool> events = [];
        monitor.ConnectivityChanged += events.Add;

        // Act
        monitor.SetOnlineStatus(false);
        monitor.SetOnlineStatus(true);
        monitor.SetOnlineStatus(false);

        // Assert
        events.Should().HaveCount(3);
        events.Should().Equal(false, true, false);
    }
}
