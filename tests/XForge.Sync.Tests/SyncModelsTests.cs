namespace XForge.Sync.Tests;

public class SyncResultTests
{
    [Fact]
    public void Success_SetsPropertiesCorrectly()
    {
        // Act
        SyncResult result = SyncResult.Success(5, 3, 1, 1, TimeSpan.FromSeconds(2));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.ItemsPushed.Should().Be(5);
        result.ItemsPulled.Should().Be(3);
        result.ConflictsDetected.Should().Be(1);
        result.ConflictsResolved.Should().Be(1);
        result.Duration.Should().Be(TimeSpan.FromSeconds(2));
        result.CompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Failure_SetsPropertiesCorrectly()
    {
        // Act
        SyncResult result = SyncResult.Failure("timeout", TimeSpan.FromSeconds(5));

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("timeout");
        result.Duration.Should().Be(TimeSpan.FromSeconds(5));
    }
}

public class ConflictResolutionTests
{
    [Fact]
    public void UseLocal_SetsCorrectType()
    {
        // Act
        ConflictResolution<int> resolution = ConflictResolution<int>.UseLocal(42);

        // Assert
        resolution.ResolutionType.Should().Be(ConflictResolutionType.UseLocal);
        resolution.ResolvedValue.Should().Be(42);
    }

    [Fact]
    public void UseRemote_SetsCorrectType()
    {
        // Act
        ConflictResolution<string> resolution = ConflictResolution<string>.UseRemote("remote");

        // Assert
        resolution.ResolutionType.Should().Be(ConflictResolutionType.UseRemote);
        resolution.ResolvedValue.Should().Be("remote");
    }

    [Fact]
    public void Merge_SetsCorrectType()
    {
        // Act
        ConflictResolution<string> resolution = ConflictResolution<string>.Merge("merged");

        // Assert
        resolution.ResolutionType.Should().Be(ConflictResolutionType.Merged);
        resolution.ResolvedValue.Should().Be("merged");
    }
}
