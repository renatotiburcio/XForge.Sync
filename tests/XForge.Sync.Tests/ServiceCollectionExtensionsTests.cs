using Microsoft.Extensions.DependencyInjection;

namespace XForge.Sync.Tests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddXForgeSync_RegistersChangeTracker()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddXForgeSync();

        // Assert
        services.Should().Contain(sd => sd.ServiceType == typeof(IChangeTracker));
    }

    [Fact]
    public void AddXForgeSync_RegistersSyncQueue()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddXForgeSync();

        // Assert
        services.Should().Contain(sd => sd.ServiceType == typeof(ISyncQueue));
    }

    [Fact]
    public void AddXForgeSync_RegistersConflictResolver()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddXForgeSync();

        // Assert
        services.Should().Contain(sd => sd.ServiceType == typeof(IConflictResolver));
    }

    [Fact]
    public void AddXForgeSync_RegistersSyncPolicy()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddXForgeSync();

        // Assert
        services.Should().Contain(sd => sd.ServiceType == typeof(ISyncPolicy));
    }

    [Fact]
    public void AddXForgeSync_RegistersConnectivityMonitor()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddXForgeSync();

        // Assert
        services.Should().Contain(sd => sd.ServiceType == typeof(IConnectivityMonitor));
    }

    [Fact]
    public void AddXForgeSync_RegistersSyncEngine()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddXForgeSync();

        // Assert
        services.Should().Contain(sd => sd.ServiceType == typeof(ISyncEngine));
    }

    [Fact]
    public void AddXForgeSync_RegistersSingletons()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddXForgeSync();

        // Assert
        ServiceProvider provider = services.BuildServiceProvider();
        IChangeTracker tracker1 = provider.GetRequiredService<IChangeTracker>();
        IChangeTracker tracker2 = provider.GetRequiredService<IChangeTracker>();
        tracker1.Should().BeSameAs(tracker2);
    }

    [Fact]
    public void AddXForgeSync_ThrowsOnNullServices()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddXForgeSync());
    }

    [Fact]
    public void AddXForgeSync_DoesNotDuplicate_WhenCalledTwice()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddXForgeSync();
        services.AddXForgeSync();

        // Assert — TryAdd should prevent duplicates
        int trackerCount = services.Count(sd => sd.ServiceType == typeof(IChangeTracker));
        trackerCount.Should().Be(1);
    }
}
