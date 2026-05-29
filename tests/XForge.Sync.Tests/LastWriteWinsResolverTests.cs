namespace XForge.Sync.Tests;

public class LastWriteWinsResolverTests
{
    [Fact]
    public async Task Resolve_LocalNewer_ReturnsUseLocal()
    {
        // Arrange
        LastWriteWinsResolver resolver = new();
        SyncConflict<string> conflict = new()
        {
            Local = "local",
            Remote = "remote",
            Key = "test:1",
            LocalVersion = 2,
            RemoteVersion = 1,
            LocalModified = DateTime.UtcNow,
            RemoteModified = DateTime.UtcNow.AddMinutes(-5)
        };

        // Act
        ConflictResolution<string> result = await resolver.ResolveAsync(conflict);

        // Assert
        result.ResolutionType.Should().Be(ConflictResolutionType.UseLocal);
        result.ResolvedValue.Should().Be("local");
    }

    [Fact]
    public async Task Resolve_RemoteNewer_ReturnsUseRemote()
    {
        // Arrange
        LastWriteWinsResolver resolver = new();
        SyncConflict<string> conflict = new()
        {
            Local = "local",
            Remote = "remote",
            Key = "test:1",
            LocalVersion = 1,
            RemoteVersion = 2,
            LocalModified = DateTime.UtcNow.AddMinutes(-5),
            RemoteModified = DateTime.UtcNow
        };

        // Act
        ConflictResolution<string> result = await resolver.ResolveAsync(conflict);

        // Assert
        result.ResolutionType.Should().Be(ConflictResolutionType.UseRemote);
        result.ResolvedValue.Should().Be("remote");
    }

    [Fact]
    public async Task Resolve_SameTime_ReturnsUseLocal()
    {
        // Arrange
        LastWriteWinsResolver resolver = new();
        DateTime now = DateTime.UtcNow;
        SyncConflict<string> conflict = new()
        {
            Local = "local",
            Remote = "remote",
            Key = "test:1",
            LocalVersion = 1,
            RemoteVersion = 1,
            LocalModified = now,
            RemoteModified = now
        };

        // Act
        ConflictResolution<string> result = await resolver.ResolveAsync(conflict);

        // Assert
        result.ResolutionType.Should().Be(ConflictResolutionType.UseLocal);
    }

    [Fact]
    public async Task Resolve_ThrowsOnNull()
    {
        // Arrange
        LastWriteWinsResolver resolver = new();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            resolver.ResolveAsync<string>(null!));
    }
}
