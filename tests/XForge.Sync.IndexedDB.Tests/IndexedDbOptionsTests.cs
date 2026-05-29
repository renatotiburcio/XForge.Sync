namespace XForge.Sync.IndexedDB.Tests;

/// <summary>
/// Tests for <see cref="IndexedDbOptions"/>.
/// </summary>
public sealed class IndexedDbOptionsTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        IndexedDbOptions options = new();

        options.DatabaseName.Should().Be("xforge_sync");
        options.Version.Should().Be(1);
        options.ItemsStoreName.Should().Be("sync_items");
        options.MetadataStoreName.Should().Be("sync_metadata");
    }

    [Fact]
    public void SectionName_IsCorrect()
    {
        IndexedDbOptions.SectionName.Should().Be("XForge:Sync:IndexedDB");
    }

    [Fact]
    public void Properties_CanBeSet()
    {
        IndexedDbOptions options = new()
        {
            DatabaseName = "my_db",
            Version = 2,
            ItemsStoreName = "items",
            MetadataStoreName = "metadata"
        };

        options.DatabaseName.Should().Be("my_db");
        options.Version.Should().Be(2);
        options.ItemsStoreName.Should().Be("items");
        options.MetadataStoreName.Should().Be("metadata");
    }
}
