namespace XForge.Sync.AspNetCore.Tests;

public class AspNetCoreSyncOptionsTests
{
    [Fact]
    public void SectionName_IsCorrect()
    {
        // Assert
        AspNetCoreSyncOptions.SectionName.Should().Be("XForge:Sync:AspNetCore");
    }

    [Fact]
    public void DefaultValues_AreCorrect()
    {
        // Act
        AspNetCoreSyncOptions options = new();

        // Assert
        options.BasePath.Should().Be("/api/sync");
        options.EnableHealthEndpoint.Should().BeTrue();
        options.EnableDetailedErrors.Should().BeFalse();
    }

    [Fact]
    public void BasePath_CanBeSet()
    {
        // Arrange
        AspNetCoreSyncOptions options = new();

        // Act
        options.BasePath = "/custom/path";

        // Assert
        options.BasePath.Should().Be("/custom/path");
    }

    [Fact]
    public void EnableHealthEndpoint_CanBeSet()
    {
        // Arrange
        AspNetCoreSyncOptions options = new();

        // Act
        options.EnableHealthEndpoint = false;

        // Assert
        options.EnableHealthEndpoint.Should().BeFalse();
    }

    [Fact]
    public void EnableDetailedErrors_CanBeSet()
    {
        // Arrange
        AspNetCoreSyncOptions options = new();

        // Act
        options.EnableDetailedErrors = true;

        // Assert
        options.EnableDetailedErrors.Should().BeTrue();
    }
}
