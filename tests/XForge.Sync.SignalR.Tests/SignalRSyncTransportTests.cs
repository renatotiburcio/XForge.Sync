using Microsoft.Extensions.DependencyInjection;
using XForge.Sync.SignalR.Extensions;

namespace XForge.Sync.SignalR.Tests;

/// <summary>
/// Tests for <see cref="SignalRSyncTransport"/>.
/// </summary>
public sealed class SignalRSyncTransportTests
{
    private static SignalRSyncTransportOptions CreateOptions(string hubUrl = "https://sync.example.com/hubs/sync") =>
        new() { HubUrl = hubUrl, MaxRetries = 0, AutomaticReconnect = false };

    private static Mock<ILogger<SignalRSyncTransport>> CreateLogger() =>
        new() { DefaultValue = DefaultValue.Mock };

    // -------------------------------------------------------------------
    // Constructor — Argument validation
    // -------------------------------------------------------------------

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Act
        Func<SignalRSyncTransport> act = () => new SignalRSyncTransport(
            null!,
            CreateLogger().Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("options");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act
        Func<SignalRSyncTransport> act = () => new SignalRSyncTransport(
            Options.Create(CreateOptions()),
            null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_ValidParameters_DoesNotThrow()
    {
        // Act
        Func<SignalRSyncTransport> act = () => new SignalRSyncTransport(
            Options.Create(CreateOptions()),
            CreateLogger().Object);

        // Assert
        act.Should().NotThrow();
    }

    // -------------------------------------------------------------------
    // Constructor — Default state
    // -------------------------------------------------------------------

    [Fact]
    public void Constructor_DefaultState_IsDisconnected()
    {
        // Arrange & Act
        SignalRSyncTransport sut = new(
            Options.Create(CreateOptions()),
            CreateLogger().Object);

        // Assert
        sut.State.Should().Be(HubConnectionState.Disconnected);
    }

    // -------------------------------------------------------------------
    // SendAsync — Argument validation
    // -------------------------------------------------------------------

    [Fact]
    public async Task SendAsync_NullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        SignalRSyncTransport sut = new(
            Options.Create(CreateOptions()),
            CreateLogger().Object);

        // Act
        Func<Task> act = () => sut.SendAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    // -------------------------------------------------------------------
    // SendAsync — Disposed state
    // -------------------------------------------------------------------

    [Fact]
    public async Task SendAsync_Disposed_ThrowsObjectDisposedException()
    {
        // Arrange
        SignalRSyncTransport sut = new(
            Options.Create(CreateOptions()),
            CreateLogger().Object);

        await sut.DisposeAsync();

        SyncRequest request = new() { ClientId = "test", LastServerVersion = 0, Changes = [] };

        // Act
        Func<Task> act = () => sut.SendAsync(request);

        // Assert
        await act.Should().ThrowAsync<ObjectDisposedException>();
    }

    // -------------------------------------------------------------------
    // IsAvailableAsync — Disposed state
    // -------------------------------------------------------------------

    [Fact]
    public async Task IsAvailableAsync_Disposed_ThrowsObjectDisposedException()
    {
        // Arrange
        SignalRSyncTransport sut = new(
            Options.Create(CreateOptions()),
            CreateLogger().Object);

        await sut.DisposeAsync();

        // Act
        Func<Task> act = () => sut.IsAvailableAsync();

        // Assert
        await act.Should().ThrowAsync<ObjectDisposedException>();
    }

    // -------------------------------------------------------------------
    // DisposeAsync — Multiple calls
    // -------------------------------------------------------------------

    [Fact]
    public async Task DisposeAsync_MultipleCalls_DoesNotThrow()
    {
        // Arrange
        SignalRSyncTransport sut = new(
            Options.Create(CreateOptions()),
            CreateLogger().Object);

        // Act
        await sut.DisposeAsync();

        // Assert
        Func<Task> act = async () => { await sut.DisposeAsync(); };
        await act.Should().NotThrowAsync();
    }

    // -------------------------------------------------------------------
    // Options — Default values
    // -------------------------------------------------------------------

    [Fact]
    public void Options_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        SignalRSyncTransportOptions options = new() { HubUrl = "https://test.com" };

        // Assert
        options.SyncMethodName.Should().Be("Sync");
        options.PingMethodName.Should().Be("Ping");
        options.TransportType.Should().Be(HttpTransportType.None);
        options.KeepAliveInterval.Should().Be(TimeSpan.FromSeconds(15));
        options.ServerTimeout.Should().Be(TimeSpan.FromSeconds(30));
        options.MaxRetries.Should().Be(3);
        options.RetryBaseDelay.Should().Be(TimeSpan.FromSeconds(1));
        options.InvokeTimeout.Should().Be(TimeSpan.FromSeconds(30));
        options.AccessTokenProvider.Should().BeNull();
        options.DefaultHeaders.Should().BeEmpty();
        options.AutomaticReconnect.Should().BeTrue();
        options.ReconnectIntervals.Should().HaveCount(4);
    }

    [Fact]
    public void Options_CustomValues_AreApplied()
    {
        // Arrange & Act
        SignalRSyncTransportOptions options = new()
        {
            HubUrl = "https://custom.com/hub",
            SyncMethodName = "CustomSync",
            PingMethodName = "CustomPing",
            TransportType = HttpTransportType.WebSockets,
            KeepAliveInterval = TimeSpan.FromSeconds(30),
            ServerTimeout = TimeSpan.FromSeconds(60),
            MaxRetries = 5,
            RetryBaseDelay = TimeSpan.FromSeconds(2),
            InvokeTimeout = TimeSpan.FromSeconds(45),
            AutomaticReconnect = false,
            ReconnectIntervals = [TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5)]
        };

        // Assert
        options.HubUrl.Should().Be("https://custom.com/hub");
        options.SyncMethodName.Should().Be("CustomSync");
        options.PingMethodName.Should().Be("CustomPing");
        options.TransportType.Should().Be(HttpTransportType.WebSockets);
        options.KeepAliveInterval.Should().Be(TimeSpan.FromSeconds(30));
        options.ServerTimeout.Should().Be(TimeSpan.FromSeconds(60));
        options.MaxRetries.Should().Be(5);
        options.RetryBaseDelay.Should().Be(TimeSpan.FromSeconds(2));
        options.InvokeTimeout.Should().Be(TimeSpan.FromSeconds(45));
        options.AutomaticReconnect.Should().BeFalse();
        options.ReconnectIntervals.Should().HaveCount(2);
    }

    [Fact]
    public void Options_SectionName_IsCorrect()
    {
        // Assert
        SignalRSyncTransportOptions.SectionName.Should().Be("XForge:Sync:SignalR");
    }

    [Fact]
    public void Options_DefaultHeaders_CanAddEntries()
    {
        // Arrange
        SignalRSyncTransportOptions options = new() { HubUrl = "https://test.com" };

        // Act
        options.DefaultHeaders["Authorization"] = "Bearer token123";
        options.DefaultHeaders["X-Custom"] = "value";

        // Assert
        options.DefaultHeaders.Should().HaveCount(2);
        options.DefaultHeaders["Authorization"].Should().Be("Bearer token123");
        options.DefaultHeaders["X-Custom"].Should().Be("value");
    }

    // -------------------------------------------------------------------
    // Events — Subscribe/Unsubscribe
    // -------------------------------------------------------------------

    [Fact]
    public void Reconnected_Event_CanSubscribe()
    {
        // Arrange
        SignalRSyncTransport sut = new(
            Options.Create(CreateOptions()),
            CreateLogger().Object);

        // Act
        static Task handler(string? _)
        {
            return Task.CompletedTask;
        }
        sut.Reconnected += handler;

        // Assert — no exception, event is subscribable
        sut.Reconnected -= handler;
    }

    [Fact]
    public void Reconnecting_Event_CanSubscribe()
    {
        // Arrange
        SignalRSyncTransport sut = new(
            Options.Create(CreateOptions()),
            CreateLogger().Object);

        // Act
        static Task handler(Exception? _)
        {
            return Task.CompletedTask;
        }
        sut.Reconnecting += handler;

        // Assert — no exception, event is subscribable
        sut.Reconnecting -= handler;
    }

    // -------------------------------------------------------------------
    // DI Extension
    // -------------------------------------------------------------------

    [Fact]
    public void AddXForgeSyncSignalR_RegistersServices()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddOptions();
        services.AddLogging();

        // Act
        services.AddXForgeSyncSignalR(opts => opts.HubUrl = "https://test.com/hub");

        // Assert
        ServiceProvider provider = services.BuildServiceProvider();
        ISyncTransport? transport = provider.GetService<ISyncTransport>();
        transport.Should().NotBeNull();
        transport.Should().BeOfType<SignalRSyncTransport>();
    }

    [Fact]
    public void AddXForgeSyncSignalR_WithConfigureAction_BindsOptions()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddOptions();
        services.AddLogging();

        // Act
        services.AddXForgeSyncSignalR(opts =>
        {
            opts.HubUrl = "https://custom.example.com/hub";
            opts.MaxRetries = 5;
            opts.KeepAliveInterval = TimeSpan.FromSeconds(30);
        });

        // Assert
        ServiceProvider provider = services.BuildServiceProvider();
        IOptions<SignalRSyncTransportOptions> options =
            provider.GetRequiredService<IOptions<SignalRSyncTransportOptions>>();
        options.Value.HubUrl.Should().Be("https://custom.example.com/hub");
        options.Value.MaxRetries.Should().Be(5);
        options.Value.KeepAliveInterval.Should().Be(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void AddXForgeSyncSignalR_SingletonRegistration()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddOptions();
        services.AddLogging();
        services.AddXForgeSyncSignalR(opts => opts.HubUrl = "https://test.com/hub");

        // Act
        ServiceProvider provider = services.BuildServiceProvider();
        ISyncTransport transport1 = provider.GetRequiredService<ISyncTransport>();
        ISyncTransport transport2 = provider.GetRequiredService<ISyncTransport>();

        // Assert
        transport1.Should().BeSameAs(transport2);
    }

    [Fact]
    public void AddXForgeSyncSignalR_TransportReturnsSignalRSyncTransport()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddOptions();
        services.AddLogging();
        services.AddXForgeSyncSignalR(opts => opts.HubUrl = "https://test.com/hub");

        // Act
        ServiceProvider provider = services.BuildServiceProvider();
        ISyncTransport transport = provider.GetRequiredService<ISyncTransport>();

        // Assert
        transport.Should().BeOfType<SignalRSyncTransport>();
    }

    // -------------------------------------------------------------------
    // DI Extension — Configuration binding
    // -------------------------------------------------------------------

    [Fact]
    public void AddXForgeSyncSignalR_WithIConfiguration_BindsOptions()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddOptions();
        services.AddLogging();

        Dictionary<string, string?> configData = new()
        {
            ["XForge:Sync:SignalR:HubUrl"] = "https://config.example.com/hub",
            ["XForge:Sync:SignalR:MaxRetries"] = "7"
        };
        Microsoft.Extensions.Configuration.ConfigurationBuilder configBuilder = new();
        configBuilder.AddInMemoryCollection(configData);
        Microsoft.Extensions.Configuration.IConfiguration configuration = configBuilder.Build();

        // Act
        services.AddXForgeSyncSignalR(configuration);

        // Assert
        ServiceProvider provider = services.BuildServiceProvider();
        IOptions<SignalRSyncTransportOptions> options =
            provider.GetRequiredService<IOptions<SignalRSyncTransportOptions>>();
        options.Value.HubUrl.Should().Be("https://config.example.com/hub");
        options.Value.MaxRetries.Should().Be(7);
    }
}
