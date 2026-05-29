using Microsoft.Extensions.DependencyInjection;
using XForge.Sync.Extensions;

namespace XForge.Sync.Tests;

public class PropertyMergeResolverTests
{
    // ───────────────────────────────────────────────────────────────────────
    // Test model
    // ───────────────────────────────────────────────────────────────────────

    public record TestEntity
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string? Description { get; init; }
        public int Quantity { get; init; }
        public bool IsActive { get; init; } = true;
    }

    public record NestedEntity
    {
        public int Id { get; init; }
        public string Title { get; init; } = string.Empty;
        public TestEntity? Child { get; init; }
    }

    // ───────────────────────────────────────────────────────────────────────
    // Null conflict throws
    // ───────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Resolve_NullConflict_Throws()
    {
        PropertyMergeResolver resolver = new();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            resolver.ResolveAsync<TestEntity>(null!));
    }

    // ───────────────────────────────────────────────────────────────────────
    // No Base → LWW fallback
    // ───────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Resolve_NoBase_LocalNewer_ReturnsUseLocal()
    {
        PropertyMergeResolver resolver = new();
        SyncConflict<TestEntity> conflict = CreateConflict(
            local: new TestEntity { Id = 1, Name = "local" },
            remote: new TestEntity { Id = 1, Name = "remote" },
            @base: null,
            localModified: DateTime.UtcNow,
            remoteModified: DateTime.UtcNow.AddMinutes(-5));

        ConflictResolution<TestEntity> result = await resolver.ResolveAsync(conflict);

        result.ResolutionType.Should().Be(ConflictResolutionType.UseLocal);
        result.ResolvedValue.Name.Should().Be("local");
    }

    [Fact]
    public async Task Resolve_NoBase_RemoteNewer_ReturnsUseRemote()
    {
        PropertyMergeResolver resolver = new();
        SyncConflict<TestEntity> conflict = CreateConflict(
            local: new TestEntity { Id = 1, Name = "local" },
            remote: new TestEntity { Id = 1, Name = "remote" },
            @base: null,
            localModified: DateTime.UtcNow.AddMinutes(-5),
            remoteModified: DateTime.UtcNow);

        ConflictResolution<TestEntity> result = await resolver.ResolveAsync(conflict);

        result.ResolutionType.Should().Be(ConflictResolutionType.UseRemote);
        result.ResolvedValue.Name.Should().Be("remote");
    }

    [Fact]
    public async Task Resolve_NoBase_SameTime_ReturnsUseLocal()
    {
        PropertyMergeResolver resolver = new();
        DateTime now = DateTime.UtcNow;
        SyncConflict<TestEntity> conflict = CreateConflict(
            local: new TestEntity { Id = 1, Name = "local" },
            remote: new TestEntity { Id = 1, Name = "remote" },
            @base: null,
            localModified: now,
            remoteModified: now);

        ConflictResolution<TestEntity> result = await resolver.ResolveAsync(conflict);

        result.ResolutionType.Should().Be(ConflictResolutionType.UseLocal);
    }

    // ───────────────────────────────────────────────────────────────────────
    // With Base — no changes
    // ───────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Resolve_BothUnchanged_ReturnsMergedWithBaseValues()
    {
        PropertyMergeResolver resolver = new();
        TestEntity entity = new() { Id = 1, Name = "original", Quantity = 10 };
        SyncConflict<TestEntity> conflict = CreateConflict(
            local: entity,
            remote: entity,
            @base: entity);

        ConflictResolution<TestEntity> result = await resolver.ResolveAsync(conflict);

        result.ResolutionType.Should().Be(ConflictResolutionType.Merged);
        result.ResolvedValue.Name.Should().Be("original");
        result.ResolvedValue.Quantity.Should().Be(10);
    }

    // ───────────────────────────────────────────────────────────────────────
    // With Base — only local changed
    // ───────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Resolve_OnlyLocalChanged_TakesLocalValue()
    {
        PropertyMergeResolver resolver = new();
        TestEntity @base = new() { Id = 1, Name = "original", Description = "base desc", Quantity = 10 };
        TestEntity local = new() { Id = 1, Name = "local changed", Description = "base desc", Quantity = 10 };
        TestEntity remote = new() { Id = 1, Name = "original", Description = "base desc", Quantity = 10 };
        SyncConflict<TestEntity> conflict = CreateConflict(local: local, remote: remote, @base: @base);

        ConflictResolution<TestEntity> result = await resolver.ResolveAsync(conflict);

        result.ResolutionType.Should().Be(ConflictResolutionType.Merged);
        result.ResolvedValue.Name.Should().Be("local changed");
        result.ResolvedValue.Description.Should().Be("base desc");
    }

    [Fact]
    public async Task Resolve_OnlyLocalChangedMultipleProps_TakesAllLocalValues()
    {
        PropertyMergeResolver resolver = new();
        TestEntity @base = new() { Id = 1, Name = "original", Description = "base", Quantity = 10 };
        TestEntity local = new() { Id = 1, Name = "changed", Description = "changed", Quantity = 99 };
        TestEntity remote = new() { Id = 1, Name = "original", Description = "base", Quantity = 10 };
        SyncConflict<TestEntity> conflict = CreateConflict(local: local, remote: remote, @base: @base);

        ConflictResolution<TestEntity> result = await resolver.ResolveAsync(conflict);

        result.ResolvedValue.Name.Should().Be("changed");
        result.ResolvedValue.Description.Should().Be("changed");
        result.ResolvedValue.Quantity.Should().Be(99);
    }

    // ───────────────────────────────────────────────────────────────────────
    // With Base — only remote changed
    // ───────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Resolve_OnlyRemoteChanged_TakesRemoteValue()
    {
        PropertyMergeResolver resolver = new();
        TestEntity @base = new() { Id = 1, Name = "original", Quantity = 10 };
        TestEntity local = new() { Id = 1, Name = "original", Quantity = 10 };
        TestEntity remote = new() { Id = 1, Name = "remote changed", Quantity = 10 };
        SyncConflict<TestEntity> conflict = CreateConflict(local: local, remote: remote, @base: @base);

        ConflictResolution<TestEntity> result = await resolver.ResolveAsync(conflict);

        result.ResolutionType.Should().Be(ConflictResolutionType.Merged);
        result.ResolvedValue.Name.Should().Be("remote changed");
    }

    // ───────────────────────────────────────────────────────────────────────
    // With Base — both changed (default: UseLocal)
    // ───────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Resolve_BothChanged_DefaultsToLocal()
    {
        PropertyMergeResolver resolver = new();
        TestEntity @base = new() { Id = 1, Name = "original" };
        TestEntity local = new() { Id = 1, Name = "local version" };
        TestEntity remote = new() { Id = 1, Name = "remote version" };
        SyncConflict<TestEntity> conflict = CreateConflict(local: local, remote: remote, @base: @base);

        ConflictResolution<TestEntity> result = await resolver.ResolveAsync(conflict);

        result.ResolutionType.Should().Be(ConflictResolutionType.Merged);
        result.ResolvedValue.Name.Should().Be("local version");
    }

    [Fact]
    public async Task Resolve_BothChanged_UseRemoteStrategy_TakesRemote()
    {
        PropertyMergeOptions options = new() { BothChangedStrategy = ConflictResolutionType.UseRemote };
        PropertyMergeResolver resolver = new(options);
        TestEntity @base = new() { Id = 1, Name = "original" };
        TestEntity local = new() { Id = 1, Name = "local version" };
        TestEntity remote = new() { Id = 1, Name = "remote version" };
        SyncConflict<TestEntity> conflict = CreateConflict(local: local, remote: remote, @base: @base);

        ConflictResolution<TestEntity> result = await resolver.ResolveAsync(conflict);

        result.ResolutionType.Should().Be(ConflictResolutionType.Merged);
        result.ResolvedValue.Name.Should().Be("remote version");
    }

    // ───────────────────────────────────────────────────────────────────────
    // Mixed changes
    // ───────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Resolve_MixedChanges_MergesCorrectly()
    {
        PropertyMergeResolver resolver = new();
        TestEntity @base = new() { Id = 1, Name = "original", Description = "base", Quantity = 10, IsActive = true };
        TestEntity local = new() { Id = 1, Name = "local changed", Description = "base", Quantity = 10, IsActive = false };
        TestEntity remote = new() { Id = 1, Name = "original", Description = "remote changed", Quantity = 99, IsActive = true };
        SyncConflict<TestEntity> conflict = CreateConflict(local: local, remote: remote, @base: @base);

        ConflictResolution<TestEntity> result = await resolver.ResolveAsync(conflict);

        result.ResolutionType.Should().Be(ConflictResolutionType.Merged);
        // Name: only local changed
        result.ResolvedValue.Name.Should().Be("local changed");
        // Description: only remote changed
        result.ResolvedValue.Description.Should().Be("remote changed");
        // Quantity: only remote changed
        result.ResolvedValue.Quantity.Should().Be(99);
        // IsActive: only local changed
        result.ResolvedValue.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Resolve_MixedWithBothChanged_MergesWithStrategy()
    {
        PropertyMergeOptions options = new() { BothChangedStrategy = ConflictResolutionType.UseRemote };
        PropertyMergeResolver resolver = new(options);
        TestEntity @base = new() { Id = 1, Name = "original", Description = "base", Quantity = 10 };
        TestEntity local = new() { Id = 1, Name = "local", Description = "base", Quantity = 50 };
        TestEntity remote = new() { Id = 1, Name = "remote", Description = "changed", Quantity = 99 };
        SyncConflict<TestEntity> conflict = CreateConflict(local: local, remote: remote, @base: @base);

        ConflictResolution<TestEntity> result = await resolver.ResolveAsync(conflict);

        // Name: both changed → UseRemote
        result.ResolvedValue.Name.Should().Be("remote");
        // Description: only remote changed
        result.ResolvedValue.Description.Should().Be("changed");
        // Quantity: both changed → UseRemote
        result.ResolvedValue.Quantity.Should().Be(99);
    }

    // ───────────────────────────────────────────────────────────────────────
    // Null property handling
    // ───────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Resolve_NullPropertyInBase_LocalSetsValue_TakesLocal()
    {
        PropertyMergeResolver resolver = new();
        TestEntity @base = new() { Id = 1, Name = "original", Description = null };
        TestEntity local = new() { Id = 1, Name = "original", Description = "new desc" };
        TestEntity remote = new() { Id = 1, Name = "original", Description = null };
        SyncConflict<TestEntity> conflict = CreateConflict(local: local, remote: remote, @base: @base);

        ConflictResolution<TestEntity> result = await resolver.ResolveAsync(conflict);

        result.ResolvedValue.Description.Should().Be("new desc");
    }

    [Fact]
    public async Task Resolve_NullPropertyInBase_BothSet_TakesLocalByDefault()
    {
        PropertyMergeResolver resolver = new();
        TestEntity @base = new() { Id = 1, Description = null };
        TestEntity local = new() { Id = 1, Description = "local desc" };
        TestEntity remote = new() { Id = 1, Description = "remote desc" };
        SyncConflict<TestEntity> conflict = CreateConflict(local: local, remote: remote, @base: @base);

        ConflictResolution<TestEntity> result = await resolver.ResolveAsync(conflict);

        result.ResolvedValue.Description.Should().Be("local desc");
    }

    [Fact]
    public async Task Resolve_PropertySetToNull_RemoteClears_TakesRemote()
    {
        PropertyMergeResolver resolver = new();
        TestEntity @base = new() { Id = 1, Description = "original" };
        TestEntity local = new() { Id = 1, Description = "original" };
        TestEntity remote = new() { Id = 1, Description = null };
        SyncConflict<TestEntity> conflict = CreateConflict(local: local, remote: remote, @base: @base);

        ConflictResolution<TestEntity> result = await resolver.ResolveAsync(conflict);

        result.ResolvedValue.Description.Should().BeNull();
    }

    // ───────────────────────────────────────────────────────────────────────
    // Boolean property changes
    // ───────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Resolve_BoolChanged_OnlyLocal_TakesLocal()
    {
        PropertyMergeResolver resolver = new();
        TestEntity @base = new() { Id = 1, IsActive = true };
        TestEntity local = new() { Id = 1, IsActive = false };
        TestEntity remote = new() { Id = 1, IsActive = true };
        SyncConflict<TestEntity> conflict = CreateConflict(local: local, remote: remote, @base: @base);

        ConflictResolution<TestEntity> result = await resolver.ResolveAsync(conflict);

        result.ResolvedValue.IsActive.Should().BeFalse();
    }

    // ───────────────────────────────────────────────────────────────────────
    // PropertyMergeOptions
    // ───────────────────────────────────────────────────────────────────────

    [Fact]
    public void PropertyMergeOptions_DefaultStrategy_IsUseLocal()
    {
        PropertyMergeOptions options = new();

        options.BothChangedStrategy.Should().Be(ConflictResolutionType.UseLocal);
    }

    [Fact]
    public void Constructor_NullOptions_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new PropertyMergeResolver(null!));
    }

    // ───────────────────────────────────────────────────────────────────────
    // CancellationToken (non-cancelled)
    // ───────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Resolve_WithCancellationToken_Succeeds()
    {
        PropertyMergeResolver resolver = new();
        TestEntity @base = new() { Id = 1, Name = "original" };
        TestEntity local = new() { Id = 1, Name = "local" };
        TestEntity remote = new() { Id = 1, Name = "remote" };
        SyncConflict<TestEntity> conflict = CreateConflict(local: local, remote: remote, @base: @base);
        using CancellationTokenSource cts = new();

        ConflictResolution<TestEntity> result = await resolver.ResolveAsync(conflict, cts.Token);

        result.ResolutionType.Should().Be(ConflictResolutionType.Merged);
    }

    // ───────────────────────────────────────────────────────────────────────
    // String type (non-record)
    // ───────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Resolve_StringType_MergesCorrectly()
    {
        PropertyMergeResolver resolver = new();
        SyncConflict<string> conflict = CreateConflict(
            local: "local value",
            remote: "remote value",
            @base: "base value");

        ConflictResolution<string> result = await resolver.ResolveAsync(conflict);

        // Both changed → default UseLocal
        result.ResolvedValue.Should().Be("local value");
    }

    [Fact]
    public async Task Resolve_StringType_OnlyLocalChanged_TakesLocal()
    {
        PropertyMergeResolver resolver = new();
        SyncConflict<string> conflict = CreateConflict(
            local: "changed",
            remote: "base value",
            @base: "base value");

        ConflictResolution<string> result = await resolver.ResolveAsync(conflict);

        result.ResolvedValue.Should().Be("changed");
    }

    // ───────────────────────────────────────────────────────────────────────
    // Multiple property conflicts with different strategies
    // ───────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Resolve_AllPropertiesConflict_UseRemoteStrategy()
    {
        PropertyMergeOptions options = new() { BothChangedStrategy = ConflictResolutionType.UseRemote };
        PropertyMergeResolver resolver = new(options);
        TestEntity @base = new() { Id = 1, Name = "base", Description = "base", Quantity = 1, IsActive = true };
        TestEntity local = new() { Id = 1, Name = "local", Description = "local", Quantity = 2, IsActive = false };
        TestEntity remote = new() { Id = 1, Name = "remote", Description = "remote", Quantity = 3, IsActive = false };
        SyncConflict<TestEntity> conflict = CreateConflict(local: local, remote: remote, @base: @base);

        ConflictResolution<TestEntity> result = await resolver.ResolveAsync(conflict);

        result.ResolutionType.Should().Be(ConflictResolutionType.Merged);
        result.ResolvedValue.Name.Should().Be("remote");
        result.ResolvedValue.Description.Should().Be("remote");
        result.ResolvedValue.Quantity.Should().Be(3);
    }

    // ───────────────────────────────────────────────────────────────────────
    // DI extension tests
    // ───────────────────────────────────────────────────────────────────────

    [Fact]
    public void UsePropertyMergeResolver_RegistersResolver()
    {
        ServiceCollection services = new();
        services.AddXForgeSync();
        services.UsePropertyMergeResolver();

        ServiceProvider provider = services.BuildServiceProvider();
        IConflictResolver resolver = provider.GetRequiredService<IConflictResolver>();

        resolver.Should().BeOfType<PropertyMergeResolver>();
    }

    [Fact]
    public void UsePropertyMergeResolver_WithOptions_AppliesOptions()
    {
        ServiceCollection services = new();
        services.AddXForgeSync();
        services.UsePropertyMergeResolver(o => o.BothChangedStrategy = ConflictResolutionType.UseRemote);

        ServiceProvider provider = services.BuildServiceProvider();
        IConflictResolver resolver = provider.GetRequiredService<IConflictResolver>();

        resolver.Should().BeOfType<PropertyMergeResolver>();
    }

    [Fact]
    public void UsePropertyMergeResolver_NullServices_Throws()
    {
        IServiceCollection? services = null;
        Assert.Throws<ArgumentNullException>(() => services!.UsePropertyMergeResolver());
    }

    // ───────────────────────────────────────────────────────────────────────
    // Large entity stress test
    // ───────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Resolve_LargeEntity_MergesCorrectly()
    {
        PropertyMergeResolver resolver = new();
        TestEntity @base = new()
        {
            Id = 42,
            Name = new string('A', 1000),
            Description = new string('B', 2000),
            Quantity = 100,
            IsActive = true
        };
        TestEntity local = new()
        {
            Id = 42,
            Name = new string('C', 1000),
            Description = new string('B', 2000),
            Quantity = 100,
            IsActive = false
        };
        TestEntity remote = new()
        {
            Id = 42,
            Name = new string('A', 1000),
            Description = new string('D', 2000),
            Quantity = 200,
            IsActive = true
        };
        SyncConflict<TestEntity> conflict = CreateConflict(local: local, remote: remote, @base: @base);

        ConflictResolution<TestEntity> result = await resolver.ResolveAsync(conflict);

        result.ResolvedValue.Name.Should().Be(new string('C', 1000)); // local changed
        result.ResolvedValue.Description.Should().Be(new string('D', 2000)); // remote changed
        result.ResolvedValue.Quantity.Should().Be(200); // remote changed
        result.ResolvedValue.IsActive.Should().BeFalse(); // local changed
    }

    // ───────────────────────────────────────────────────────────────────────
    // Integration with SyncEngine — verify resolver is wired
    // ───────────────────────────────────────────────────────────────────────

    [Fact]
    public void AddXForgeSync_DefaultResolver_IsLastWriteWins()
    {
        ServiceCollection services = new();
        services.AddXForgeSync();

        ServiceProvider provider = services.BuildServiceProvider();
        IConflictResolver resolver = provider.GetRequiredService<IConflictResolver>();

        resolver.Should().BeOfType<LastWriteWinsResolver>();
    }

    [Fact]
    public void AddXForgeSync_ThenUsePropertyMergeResolver_Overrides()
    {
        ServiceCollection services = new();
        services.AddXForgeSync();
        services.UsePropertyMergeResolver();

        ServiceProvider provider = services.BuildServiceProvider();
        IConflictResolver resolver = provider.GetRequiredService<IConflictResolver>();

        resolver.Should().BeOfType<PropertyMergeResolver>();
    }

    // ───────────────────────────────────────────────────────────────────────
    // Helpers
    // ───────────────────────────────────────────────────────────────────────

    private static SyncConflict<T> CreateConflict<T>(
        T local,
        T remote,
        T? @base,
        DateTime? localModified = null,
        DateTime? remoteModified = null)
    {
        return new SyncConflict<T>
        {
            Local = local,
            Remote = remote,
            Base = @base,
            Key = "test:1",
            LocalVersion = 1,
            RemoteVersion = 2,
            LocalModified = localModified ?? DateTime.UtcNow,
            RemoteModified = remoteModified ?? DateTime.UtcNow
        };
    }
}
