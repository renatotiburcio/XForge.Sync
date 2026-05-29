using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using Moq;
using XForge.Sync.IndexedDB.Extensions;
using XForge.Sync.Storage;

namespace XForge.Sync.IndexedDB.Tests;

/// <summary>
/// Tests for <see cref="IndexedDbServiceCollectionExtensions"/>.
/// </summary>
public sealed class IndexedDbServiceCollectionExtensionsTests
{
    // -------------------------------------------------------------------
    // AddXForgeSyncIndexedDb (no config)
    // -------------------------------------------------------------------

    [Fact]
    public void AddXForgeSyncIndexedDb_RegistersLocalStorage()
    {
        ServiceCollection services = new();
        services.AddOptions();
        services.AddLogging();
        services.AddSingleton(Mock.Of<IJSRuntime>());
        services.AddXForgeSyncIndexedDb();

        ServiceProvider provider = services.BuildServiceProvider();
        ILocalStorage? storage = provider.GetService<ILocalStorage>();

        storage.Should().NotBeNull();
        storage.Should().BeOfType<IndexedDbLocalStorage>();
    }

    [Fact]
    public void AddXForgeSyncIndexedDb_WithConfigure_AppliesOptions()
    {
        ServiceCollection services = new();
        services.AddOptions();
        services.AddLogging();
        services.AddSingleton(Mock.Of<IJSRuntime>());
        services.AddXForgeSyncIndexedDb(opts =>
        {
            opts.DatabaseName = "custom_db";
            opts.Version = 3;
        });

        ServiceProvider provider = services.BuildServiceProvider();
        IOptions<IndexedDbOptions> options = provider.GetRequiredService<IOptions<IndexedDbOptions>>();

        options.Value.DatabaseName.Should().Be("custom_db");
        options.Value.Version.Should().Be(3);
    }

    [Fact]
    public void AddXForgeSyncIndexedDb_NoConfigure_RegistersDefaultOptions()
    {
        ServiceCollection services = new();
        services.AddOptions();
        services.AddLogging();
        services.AddSingleton(Mock.Of<IJSRuntime>());
        services.AddXForgeSyncIndexedDb();

        ServiceProvider provider = services.BuildServiceProvider();
        IOptions<IndexedDbOptions> options = provider.GetRequiredService<IOptions<IndexedDbOptions>>();

        options.Value.DatabaseName.Should().Be("xforge_sync");
        options.Value.Version.Should().Be(1);
        options.Value.ItemsStoreName.Should().Be("sync_items");
        options.Value.MetadataStoreName.Should().Be("sync_metadata");
    }

    [Fact]
    public void AddXForgeSyncIndexedDb_NullServices_Throws()
    {
        Action act = () => ((IServiceCollection)null!).AddXForgeSyncIndexedDb();

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddXForgeSyncIndexedDb_TryAdd_DoesNotOverride()
    {
        ServiceCollection services = new();
        services.AddOptions();
        services.AddLogging();
        services.AddSingleton(Mock.Of<IJSRuntime>());

        // Register custom ILocalStorage first
        Mock<ILocalStorage> customStorage = new();
        services.AddSingleton<ILocalStorage>(customStorage.Object);

        // AddXForgeSyncIndexedDb should NOT override
        services.AddXForgeSyncIndexedDb();

        ServiceProvider provider = services.BuildServiceProvider();
        ILocalStorage? storage = provider.GetService<ILocalStorage>();

        storage.Should().BeSameAs(customStorage.Object);
    }

    // -------------------------------------------------------------------
    // AddXForgeSyncIndexedDb (IConfiguration)
    // -------------------------------------------------------------------

    [Fact]
    public void AddXForgeSyncIndexedDb_WithConfiguration_BindsOptions()
    {
        Dictionary<string, string?> configData = new()
        {
            ["XForge:Sync:IndexedDB:DatabaseName"] = "config_db",
            ["XForge:Sync:IndexedDB:Version"] = "5",
            ["XForge:Sync:IndexedDB:ItemsStoreName"] = "my_items",
            ["XForge:Sync:IndexedDB:MetadataStoreName"] = "my_metadata"
        };

        ConfigurationBuilder configBuilder = new();
        configBuilder.AddInMemoryCollection(configData);
        IConfiguration configuration = configBuilder.Build();

        ServiceCollection services = new();
        services.AddOptions();
        services.AddLogging();
        services.AddSingleton(Mock.Of<IJSRuntime>());
        services.AddXForgeSyncIndexedDb(configuration);

        ServiceProvider provider = services.BuildServiceProvider();
        IOptions<IndexedDbOptions> options = provider.GetRequiredService<IOptions<IndexedDbOptions>>();

        options.Value.DatabaseName.Should().Be("config_db");
        options.Value.Version.Should().Be(5);
        options.Value.ItemsStoreName.Should().Be("my_items");
        options.Value.MetadataStoreName.Should().Be("my_metadata");
    }

    [Fact]
    public void AddXForgeSyncIndexedDb_NullConfiguration_Throws()
    {
        ServiceCollection services = new();

        Action act = () => services.AddXForgeSyncIndexedDb((IConfiguration)null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
