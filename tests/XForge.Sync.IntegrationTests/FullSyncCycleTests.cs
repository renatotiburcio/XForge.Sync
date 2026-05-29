using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using XForge.Sync.Sqlite;
using XForge.Sync.Sqlite.Extensions;

namespace XForge.Sync.IntegrationTests;

/// <summary>
/// Integration tests exercising the full sync cycle.
/// </summary>
public class FullSyncCycleTests
{
    [Fact]
    public async Task FullCycle_Push_PersistsToServer()
    {
        await using SqliteConnection clientConn = new("DataSource=:memory:");
        await clientConn.OpenAsync();
        SqliteLocalStorage storage = new(
            clientConn,
            Microsoft.Extensions.Options.Options.Create(new SqliteSyncOptions { AutoCreateTables = true }),
            NullLogger<SqliteLocalStorage>.Instance);

        TestEntity entity = new() { Id = 1, Name = "Created on client" };
        await storage.SetAsync("entity:1", entity);

        InMemoryServerSyncHandler serverHandler = new();
        InMemorySyncTransport transport = new(serverHandler);

        SyncRequest request = new()
        {
            ClientId = "test-client",
            LastServerVersion = 0,
            Changes =
            [
                new TrackedChange
                {
                    Key = "entity:1",
                    Data = System.Text.Json.JsonSerializer.Serialize(entity),
                    EntityType = nameof(TestEntity),
                    ChangeType = ChangeType.Create,
                    TrackedAt = DateTime.UtcNow,
                    Version = 1
                }
            ]
        };

        SyncResponse response = await transport.SendAsync(request);

        response.IsSuccess.Should().BeTrue();
        response.ServerVersion.Should().Be(1);
        serverHandler.EntityCount.Should().Be(1);
    }

    [Fact]
    public async Task FullCycle_Pull_ReceivesServerChanges()
    {
        InMemoryServerSyncHandler serverHandler = new();
        serverHandler.Seed("entity:1", """{"Id":1,"Name":"Server entity"}""", version: 1);
        InMemorySyncTransport transport = new(serverHandler);

        SyncRequest request = new()
        {
            ClientId = "test-client",
            LastServerVersion = 0,
            Changes = []
        };

        SyncResponse response = await transport.SendAsync(request);

        response.IsSuccess.Should().BeTrue();
        response.Changes.Should().HaveCount(1);
        response.Changes[0].Key.Should().Be("entity:1");
    }

    [Fact]
    public async Task FullCycle_ConflictResolved_UsesPropertyMerge()
    {
        InMemoryServerSyncHandler serverHandler = new();
        serverHandler.Seed("entity:1", """{"Id":1,"Name":"server version","Description":"server"}""", version: 1);

        InMemorySyncTransport transport = new(serverHandler);
        PropertyMergeResolver resolver = new();

        SyncRequest pushRequest = new()
        {
            ClientId = "test-client",
            LastServerVersion = 0,
            Changes =
            [
                new TrackedChange
                {
                    Key = "entity:1",
                    Data = """{"Id":1,"Name":"client version","Description":"client"}""",
                    EntityType = nameof(TestEntity),
                    ChangeType = ChangeType.Update,
                    TrackedAt = DateTime.UtcNow,
                    Version = 1
                }
            ]
        };

        SyncResponse pushResponse = await transport.SendAsync(pushRequest);

        pushResponse.IsSuccess.Should().BeTrue();
        pushResponse.Conflicts.Should().HaveCount(1);

        SyncConflict<object> conflict = pushResponse.Conflicts[0];
        ConflictResolution<object> resolution = await resolver.ResolveAsync(conflict);

        // Without Base, falls back to LWW (remote is newer)
        resolution.ResolutionType.Should().Be(ConflictResolutionType.UseRemote);
    }

    [Fact]
    public async Task Transport_HealthCheck_ReturnsTrue()
    {
        InMemoryServerSyncHandler serverHandler = new();
        InMemorySyncTransport transport = new(serverHandler);

        bool isAvailable = await transport.IsAvailableAsync();

        isAvailable.Should().BeTrue();
    }

    [Fact]
    public async Task SqliteStorage_SetGetDelete_RoundTrip()
    {
        await using SqliteConnection conn = new("DataSource=:memory:");
        await conn.OpenAsync();
        SqliteLocalStorage storage = new(
            conn,
            Microsoft.Extensions.Options.Options.Create(new SqliteSyncOptions { AutoCreateTables = true }),
            NullLogger<SqliteLocalStorage>.Instance);

        TestEntity entity = new() { Id = 42, Name = "Test" };

        await storage.SetAsync("key:1", entity);

        TestEntity? retrieved = await storage.GetAsync<TestEntity>("key:1");
        retrieved.Should().NotBeNull();
        retrieved!.Id.Should().Be(42);
        retrieved.Name.Should().Be("Test");

        SyncMetadata? metadata = await storage.GetMetadataAsync("key:1");
        metadata.Should().NotBeNull();
        metadata!.Key.Should().Be("key:1");
        metadata.Version.Should().Be(1);
        metadata.IsDirty.Should().BeTrue();

        await storage.DeleteAsync("key:1");
        TestEntity? deleted = await storage.GetAsync<TestEntity>("key:1");
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task ChangeTracker_TrackAndRetrieve_WorksCorrectly()
    {
        await using SqliteConnection conn = new("DataSource=:memory:");
        await conn.OpenAsync();
        SqliteChangeTracker tracker = new(
            conn,
            Microsoft.Extensions.Options.Options.Create(new SqliteSyncOptions { AutoCreateTables = true }),
            NullLogger<SqliteChangeTracker>.Instance);

        TestEntity entity = new() { Id = 1, Name = "Tracked" };

        await tracker.TrackChangeAsync(entity, ChangeType.Create);

        var pending = await tracker.GetPendingChangesAsync();
        pending.Should().HaveCount(1);
        pending[0].ChangeType.Should().Be(ChangeType.Create);

        await tracker.MarkSyncedAsync(pending);

        var afterSync = await tracker.GetPendingChangesAsync();
        afterSync.Should().BeEmpty();
    }

    [Fact]
    public async Task FullCycle_MultipleEntities_AllSynced()
    {
        InMemoryServerSyncHandler serverHandler = new();
        InMemorySyncTransport transport = new(serverHandler);

        List<TrackedChange> changes = [];
        for (int i = 1; i <= 10; i++)
        {
            TestEntity entity = new() { Id = i, Name = $"Entity {i}", Quantity = i * 10 };
            changes.Add(new TrackedChange
            {
                Key = $"entity:{i}",
                Data = System.Text.Json.JsonSerializer.Serialize(entity),
                EntityType = nameof(TestEntity),
                ChangeType = ChangeType.Create,
                TrackedAt = DateTime.UtcNow,
                Version = i
            });
        }

        SyncRequest request = new()
        {
            ClientId = "test-client",
            LastServerVersion = 0,
            Changes = changes
        };

        SyncResponse response = await transport.SendAsync(request);

        response.IsSuccess.Should().BeTrue();
        response.ServerVersion.Should().Be(10);
        serverHandler.EntityCount.Should().Be(10);
    }

    [Fact]
    public async Task FullCycle_DeleteChange_SyncedToServer()
    {
        InMemoryServerSyncHandler serverHandler = new();
        serverHandler.Seed("entity:1", """{"Id":1,"Name":"to delete"}""", version: 1);
        InMemorySyncTransport transport = new(serverHandler);

        SyncRequest request = new()
        {
            ClientId = "test-client",
            LastServerVersion = 1,
            Changes =
            [
                new TrackedChange
                {
                    Key = "entity:1",
                    Data = "",
                    EntityType = nameof(TestEntity),
                    ChangeType = ChangeType.Delete,
                    TrackedAt = DateTime.UtcNow,
                    Version = 2
                }
            ]
        };

        SyncResponse response = await transport.SendAsync(request);

        response.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void AddXForgeSync_RegistersAllRequiredServices()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddSingleton<ISyncTransport, InMemorySyncTransport>();
        services.AddSingleton<InMemoryServerSyncHandler>();
        services.AddXForgeSync();

        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<ISyncEngine>().Should().NotBeNull();
        provider.GetRequiredService<IChangeTracker>().Should().NotBeNull();
        provider.GetRequiredService<ISyncQueue>().Should().NotBeNull();
        provider.GetRequiredService<IConflictResolver>().Should().NotBeNull();
        provider.GetRequiredService<ISyncPolicy>().Should().NotBeNull();
        provider.GetRequiredService<IConnectivityMonitor>().Should().NotBeNull();
    }

    [Fact]
    public void AddXForgeSyncSqlite_RegistersLocalStorage()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddXForgeSyncSqliteShared("DataSource=:memory:");
        services.UsePropertyMergeResolver();

        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<ILocalStorage>().Should().BeOfType<SqliteLocalStorage>();
        provider.GetRequiredService<IConflictResolver>().Should().BeOfType<PropertyMergeResolver>();
    }

    [Fact]
    public async Task ConflictResolution_LWW_PicksNewerTimestamp()
    {
        LastWriteWinsResolver lww = new();
        TestEntity local = new() { Id = 1, Name = "local" };
        TestEntity remote = new() { Id = 1, Name = "remote" };
        SyncConflict<TestEntity> conflict = new()
        {
            Local = local,
            Remote = remote,
            Key = "test:1",
            LocalModified = DateTime.UtcNow.AddMinutes(-10),
            RemoteModified = DateTime.UtcNow
        };

        ConflictResolution<TestEntity> result = await lww.ResolveAsync(conflict);

        result.ResolutionType.Should().Be(ConflictResolutionType.UseRemote);
        result.ResolvedValue.Name.Should().Be("remote");
    }

    [Fact]
    public async Task ConflictResolution_PropertyMerge_MergesPerProperty()
    {
        PropertyMergeResolver merge = new();
        TestEntity @base = new() { Id = 1, Name = "original", Description = "base" };
        TestEntity local = new() { Id = 1, Name = "local", Description = "base" };
        TestEntity remote = new() { Id = 1, Name = "original", Description = "remote" };
        SyncConflict<TestEntity> conflict = new()
        {
            Local = local,
            Remote = remote,
            Base = @base,
            Key = "test:1",
            LocalModified = DateTime.UtcNow,
            RemoteModified = DateTime.UtcNow
        };

        ConflictResolution<TestEntity> result = await merge.ResolveAsync(conflict);

        result.ResolutionType.Should().Be(ConflictResolutionType.Merged);
        result.ResolvedValue.Name.Should().Be("local");
        result.ResolvedValue.Description.Should().Be("remote");
    }

    public record TestEntity
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string? Description { get; init; }
        public int Quantity { get; init; }
        public bool IsActive { get; init; } = true;
    }
}
