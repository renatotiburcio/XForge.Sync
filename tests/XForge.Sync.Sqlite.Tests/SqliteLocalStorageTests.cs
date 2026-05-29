using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using XForge.Sync.Sqlite.Extensions;

namespace XForge.Sync.Sqlite.Tests;

/// <summary>
/// Tests for <see cref="SqliteLocalStorage"/>.
/// </summary>
public sealed class SqliteLocalStorageTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly SqliteLocalStorage _storage;

    public SqliteLocalStorageTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        SqliteSyncOptions options = new()
        {
            ConnectionString = "DataSource=:memory:",
            AutoCreateTables = true
        };

        _storage = new SqliteLocalStorage(
            _connection,
            Microsoft.Extensions.Options.Options.Create(options),
            new Moq.Mock<Microsoft.Extensions.Logging.ILogger<SqliteLocalStorage>>().Object);
    }

    // -------------------------------------------------------------------
    // GetAsync / SetAsync — Roundtrip
    // -------------------------------------------------------------------

    [Fact]
    public async Task SetAsync_ThenGetAsync_ReturnsValue()
    {
        TestItem item = new() { Id = 1, Name = "Test Item" };

        await _storage.SetAsync("item:1", item);
        TestItem? result = await _storage.GetAsync<TestItem>("item:1");

        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Name.Should().Be("Test Item");
    }

    [Fact]
    public async Task GetAsync_NonExistentKey_ReturnsDefault()
    {
        TestItem? result = await _storage.GetAsync<TestItem>("nonexistent");

        result.Should().BeNull();
    }

    [Fact]
    public async Task SetAsync_OverwritesExistingValue()
    {
        TestItem original = new() { Id = 1, Name = "Original" };
        TestItem updated = new() { Id = 1, Name = "Updated" };

        await _storage.SetAsync("item:1", original);
        await _storage.SetAsync("item:1", updated);
        TestItem? result = await _storage.GetAsync<TestItem>("item:1");

        result.Should().NotBeNull();
        result!.Name.Should().Be("Updated");
    }

    // -------------------------------------------------------------------
    // DeleteAsync
    // -------------------------------------------------------------------

    [Fact]
    public async Task DeleteAsync_RemovesItem()
    {
        TestItem item = new() { Id = 1, Name = "ToDelete" };
        await _storage.SetAsync("item:1", item);

        await _storage.DeleteAsync("item:1");
        TestItem? result = await _storage.GetAsync<TestItem>("item:1");

        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_NonExistentKey_DoesNotThrow()
    {
        Func<Task> act = () => _storage.DeleteAsync("nonexistent");

        await act.Should().NotThrowAsync();
    }

    // -------------------------------------------------------------------
    // GetAllAsync
    // -------------------------------------------------------------------

    [Fact]
    public async Task GetAllAsync_ReturnsAllItemsOfType()
    {
        await _storage.SetAsync("item:1", new TestItem { Id = 1, Name = "A" });
        await _storage.SetAsync("item:2", new TestItem { Id = 2, Name = "B" });
        await _storage.SetAsync("item:3", new TestItem { Id = 3, Name = "C" });
        await _storage.SetAsync("other:1", new OtherItem { Id = 100, Label = "X" });

        IReadOnlyList<TestItem> results = await _storage.GetAllAsync<TestItem>();

        results.Should().HaveCount(3);
        results.Should().Contain(i => i.Name == "A");
        results.Should().Contain(i => i.Name == "B");
        results.Should().Contain(i => i.Name == "C");
    }

    [Fact]
    public async Task GetAllAsync_EmptyStore_ReturnsEmptyList()
    {
        IReadOnlyList<TestItem> results = await _storage.GetAllAsync<TestItem>();

        results.Should().BeEmpty();
    }

    // -------------------------------------------------------------------
    // GetMetadataAsync
    // -------------------------------------------------------------------

    [Fact]
    public async Task GetMetadataAsync_AfterSet_ReturnsMetadata()
    {
        TestItem item = new() { Id = 1, Name = "WithMeta" };
        await _storage.SetAsync("item:1", item);

        SyncMetadata? metadata = await _storage.GetMetadataAsync("item:1");

        metadata.Should().NotBeNull();
        metadata!.Key.Should().Be("item:1");
        metadata.Version.Should().Be(1);
        metadata.IsDirty.Should().BeTrue();
        metadata.Checksum.Should().NotBeNullOrEmpty();
        metadata.LastModified.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GetMetadataAsync_NonExistentKey_ReturnsNull()
    {
        SyncMetadata? metadata = await _storage.GetMetadataAsync("nonexistent");

        metadata.Should().BeNull();
    }

    [Fact]
    public async Task GetMetadataAsync_IncrementsVersionOnUpdate()
    {
        await _storage.SetAsync("item:1", new TestItem { Id = 1, Name = "V1" });
        await _storage.SetAsync("item:1", new TestItem { Id = 1, Name = "V2" });

        SyncMetadata? metadata = await _storage.GetMetadataAsync("item:1");

        metadata.Should().NotBeNull();
        metadata!.Version.Should().Be(2);
    }

    // -------------------------------------------------------------------
    // MarkSyncedAsync
    // -------------------------------------------------------------------

    [Fact]
    public async Task MarkSyncedAsync_ClearsDirtyFlag()
    {
        await _storage.SetAsync("item:1", new TestItem { Id = 1, Name = "Test" });

        // Mark as synced via the storage (accessing internal method through cast)
        if (_storage is SqliteLocalStorage sqliteStorage)
        {
            await sqliteStorage.MarkSyncedAsync("item:1");
        }

        SyncMetadata? metadata = await _storage.GetMetadataAsync("item:1");

        metadata.Should().NotBeNull();
        metadata!.IsDirty.Should().BeFalse();
        metadata.LastSynced.Should().NotBeNull();
        metadata.LastSynced.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    // -------------------------------------------------------------------
    // Argument validation
    // -------------------------------------------------------------------

    [Fact]
    public async Task GetAsync_NullKey_Throws()
    {
        Func<Task> act = () => _storage.GetAsync<TestItem>(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GetAsync_EmptyKey_Throws()
    {
        Func<Task> act = () => _storage.GetAsync<TestItem>("");

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task SetAsync_NullKey_Throws()
    {
        Func<Task> act = () => _storage.SetAsync(null!, new TestItem { Id = 1 });

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task SetAsync_NullValue_Throws()
    {
        Func<Task> act = () => _storage.SetAsync<TestItem>("key", null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task DeleteAsync_NullKey_Throws()
    {
        Func<Task> act = () => _storage.DeleteAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    // -------------------------------------------------------------------
    // DI Extension
    // -------------------------------------------------------------------

    [Fact]
    public void AddXForgeSyncSqlite_RegistersServices()
    {
        ServiceCollection services = new();
        services.AddOptions();
        services.AddLogging();
        services.AddXForgeSyncSqlite(opts => opts.ConnectionString = "DataSource=:memory:");

        ServiceProvider provider = services.BuildServiceProvider();
        Storage.ILocalStorage? storage = provider.GetService<Storage.ILocalStorage>();

        storage.Should().NotBeNull();
        storage.Should().BeOfType<SqliteLocalStorage>();
    }

    [Fact]
    public void AddXForgeSyncSqlite_RegistersChangeTracker()
    {
        ServiceCollection services = new();
        services.AddOptions();
        services.AddLogging();
        services.AddXForgeSyncSqlite(opts => opts.ConnectionString = "DataSource=:memory:");

        ServiceProvider provider = services.BuildServiceProvider();
        Tracking.IChangeTracker? tracker = provider.GetService<Tracking.IChangeTracker>();

        tracker.Should().NotBeNull();
        tracker.Should().BeOfType<SqliteChangeTracker>();
    }

    // -------------------------------------------------------------------
    // Concurrent access
    // -------------------------------------------------------------------

    [Fact]
    public async Task SetAsync_ConcurrentWrites_DoNotThrow()
    {
        IEnumerable<Task> tasks = Enumerable.Range(1, 10).Select(i =>
            _storage.SetAsync($"item:{i}", new TestItem { Id = i, Name = $"Item {i}" }));

        Func<Task> act = () => Task.WhenAll(tasks);

        await act.Should().NotThrowAsync();

        IReadOnlyList<TestItem> all = await _storage.GetAllAsync<TestItem>();
        all.Should().HaveCount(10);
    }

    public void Dispose()
    {
        _storage.Dispose();
        _connection.Dispose();
    }

    // -------------------------------------------------------------------
    // Test models
    // -------------------------------------------------------------------

    private sealed record TestItem
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
    }

    private sealed record OtherItem
    {
        public int Id { get; init; }
        public string Label { get; init; } = string.Empty;
    }
}
