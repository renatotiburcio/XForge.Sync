using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using XForge.Sync.AspNetCore.Extensions;

namespace XForge.Sync.AspNetCore.Tests;

public class AspNetCoreExtensionsTests
{
    [Fact]
    public void AddXForgeSyncServer_WithNullServices_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => AspNetCoreExtensions.AddXForgeSyncServer(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddXForgeSyncServer_RegistersServices()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        IServiceCollection result = services.AddXForgeSyncServer();

        // Assert
        result.Should().BeSameAs(services);
    }

    [Fact]
    public void AddXForgeSyncServer_WithConfigure_AppliesConfiguration()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddXForgeSyncServer(opts =>
        {
            opts.BasePath = "/custom/sync";
            opts.EnableHealthEndpoint = false;
            opts.EnableDetailedErrors = true;
        });

        // Assert
        ServiceProvider provider = services.BuildServiceProvider();
        IOptions<AspNetCoreSyncOptions> options = provider.GetRequiredService<IOptions<AspNetCoreSyncOptions>>();
        options.Value.BasePath.Should().Be("/custom/sync");
        options.Value.EnableHealthEndpoint.Should().BeFalse();
        options.Value.EnableDetailedErrors.Should().BeTrue();
    }

    [Fact]
    public void AddXForgeSyncServer_WithoutConfigure_UsesDefaultOptions()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddXForgeSyncServer();

        // Assert
        ServiceProvider provider = services.BuildServiceProvider();
        IOptions<AspNetCoreSyncOptions> options = provider.GetRequiredService<IOptions<AspNetCoreSyncOptions>>();
        options.Value.BasePath.Should().Be("/api/sync");
        options.Value.EnableHealthEndpoint.Should().BeTrue();
        options.Value.EnableDetailedErrors.Should().BeFalse();
    }

    [Fact]
    public void AddXForgeSyncServer_WithNullConfiguration_ThrowsArgumentNullException()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        Action act = () => services.AddXForgeSyncServer((IConfiguration)null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddXForgeSyncServer_WithConfiguration_BindsFromSection()
    {
        // Arrange
        ServiceCollection services = new();
        Dictionary<string, string?> configData = new()
        {
            ["XForge:Sync:AspNetCore:BasePath"] = "/api/v2/sync",
            ["XForge:Sync:AspNetCore:EnableHealthEndpoint"] = "false",
            ["XForge:Sync:AspNetCore:EnableDetailedErrors"] = "true"
        };
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        // Act
        services.AddXForgeSyncServer(configuration);

        // Assert
        ServiceProvider provider = services.BuildServiceProvider();
        IOptions<AspNetCoreSyncOptions> options = provider.GetRequiredService<IOptions<AspNetCoreSyncOptions>>();
        options.Value.BasePath.Should().Be("/api/v2/sync");
        options.Value.EnableHealthEndpoint.Should().BeFalse();
        options.Value.EnableDetailedErrors.Should().BeTrue();
    }

    [Fact]
    public void AddXForgeSyncServer_ReturnsSameServiceCollection()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        IServiceCollection result = services.AddXForgeSyncServer(opts => { });

        // Assert
        result.Should().BeSameAs(services);
    }
}
