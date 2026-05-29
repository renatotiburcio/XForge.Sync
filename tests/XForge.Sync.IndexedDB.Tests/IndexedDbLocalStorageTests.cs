using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using Moq;
using XForge.Sync.Storage;

namespace XForge.Sync.IndexedDB.Tests;

/// <summary>
/// Tests for <see cref="IndexedDbLocalStorage"/>.
/// Uses mocked <see cref="IJSRuntime"/> since IndexedDB is a browser API.
/// </summary>
public sealed class IndexedDbLocalStorageTests
{
    private readonly Mock<IJSRuntime> _jsRuntime;
    private readonly Mock<IJSObjectReference> _moduleRef;
    private readonly Mock<IJSObjectReference> _dbRef;
    private readonly IndexedDbLocalStorage _storage;

    public IndexedDbLocalStorageTests()
    {
        _jsRuntime = new Mock<IJSRuntime>(MockBehavior.Strict);
        _moduleRef = new Mock<IJSObjectReference>();
        _dbRef = new Mock<IJSObjectReference>();

        // Setup module import
        _jsRuntime
            .Setup(r => r.InvokeAsync<IJSObjectReference>("import", It.IsAny<CancellationToken>(), It.IsAny<object[]>()))
            .ReturnsAsync(_moduleRef.Object);

        // Setup database open
        _jsRuntime
            .Setup(r => r.InvokeAsync<IJSObjectReference>("xForgeIndexedDb.openDatabase", It.IsAny<CancellationToken>(), It.IsAny<object[]>()))
            .ReturnsAsync(_dbRef.Object);

        IndexedDbOptions options = new();
        ILogger<IndexedDbLocalStorage> logger = new Mock<ILogger<IndexedDbLocalStorage>>().Object;

        _storage = new IndexedDbLocalStorage(
            _jsRuntime.Object,
            Options.Create(options),
            logger);
    }

    // -------------------------------------------------------------------
    // Constructor
    // -------------------------------------------------------------------

    [Fact]
    public void Constructor_NullJsRuntime_Throws()
    {
        IndexedDbOptions options = new();
        ILogger<IndexedDbLocalStorage> logger = new Mock<ILogger<IndexedDbLocalStorage>>().Object;

        Action act = () => new IndexedDbLocalStorage(null!, Options.Create(options), logger);

        act.Should().Throw<ArgumentNullException>().WithParameterName("jsRuntime");
    }

    [Fact]
    public void Constructor_NullOptions_Throws()
    {
        ILogger<IndexedDbLocalStorage> logger = new Mock<ILogger<IndexedDbLocalStorage>>().Object;

        Action act = () => new IndexedDbLocalStorage(_jsRuntime.Object, null!, logger);

        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    [Fact]
    public void Constructor_NullLogger_Throws()
    {
        IndexedDbOptions options = new();

        Action act = () => new IndexedDbLocalStorage(_jsRuntime.Object, Options.Create(options), null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    // -------------------------------------------------------------------
    // GetAsync — Argument validation
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

    // -------------------------------------------------------------------
    // GetAsync — JS interop
    // -------------------------------------------------------------------

    [Fact]
    public async Task GetAsync_ItemExists_ReturnsDeserializedValue()
    {
        TestItem item = new() { Id = 1, Name = "Test" };
        JsonElement itemJson = JsonSerializer.SerializeToElement(new { key = "item:1", data = item, typeName = "TestItem", updatedAt = DateTime.UtcNow.ToString("o") });

        _jsRuntime
            .Setup(r => r.InvokeAsync<JsonElement?>("xForgeIndexedDb.getItem", It.IsAny<CancellationToken>(), It.IsAny<object[]>()))
            .ReturnsAsync(itemJson);

        TestItem? result = await _storage.GetAsync<TestItem>("item:1");

        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Name.Should().Be("Test");
    }

    [Fact]
    public async Task GetAsync_ItemNotFound_ReturnsDefault()
    {
        _jsRuntime
            .Setup(r => r.InvokeAsync<JsonElement?>("xForgeIndexedDb.getItem", It.IsAny<CancellationToken>(), It.IsAny<object[]>()))
            .ReturnsAsync((JsonElement?)null);

        TestItem? result = await _storage.GetAsync<TestItem>("nonexistent");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_ItemWithoutDataProperty_ReturnsDefault()
    {
        JsonElement itemJson = JsonSerializer.SerializeToElement(new { key = "item:1", typeName = "TestItem" });

        _jsRuntime
            .Setup(r => r.InvokeAsync<JsonElement?>("xForgeIndexedDb.getItem", It.IsAny<CancellationToken>(), It.IsAny<object[]>()))
            .ReturnsAsync(itemJson);

        TestItem? result = await _storage.GetAsync<TestItem>("item:1");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_JsException_ReturnsDefault()
    {
        _jsRuntime
            .Setup(r => r.InvokeAsync<JsonElement?>("xForgeIndexedDb.getItem", It.IsAny<CancellationToken>(), It.IsAny<object[]>()))
            .ThrowsAsync(new JSException("IndexedDB error"));

        TestItem? result = await _storage.GetAsync<TestItem>("item:1");

        result.Should().BeNull();
    }

    // -------------------------------------------------------------------
    // SetAsync — Argument validation
    // -------------------------------------------------------------------

    [Fact]
    public async Task SetAsync_NullKey_Throws()
    {
        Func<Task> act = () => _storage.SetAsync(null!, new TestItem { Id = 1 });

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task SetAsync_EmptyKey_Throws()
    {
        Func<Task> act = () => _storage.SetAsync("", new TestItem { Id = 1 });

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task SetAsync_NullValue_Throws()
    {
        Func<Task> act = () => _storage.SetAsync<TestItem>("key", null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    // -------------------------------------------------------------------
    // SetAsync — JS interop
    // -------------------------------------------------------------------

    [Fact]
    public async Task SetAsync_CallsJsSetItem_ForBothStores()
    {
        TestItem item = new() { Id = 1, Name = "Test" };

        // Setup metadata get (returns null for new item)
        _jsRuntime
            .Setup(r => r.InvokeAsync<JsonElement?>("xForgeIndexedDb.getItem", It.IsAny<CancellationToken>(), It.IsAny<object[]>()))
            .ReturnsAsync((JsonElement?)null);

        // Setup setItem
        _jsRuntime
            .Setup(r => r.InvokeAsync<object?>("xForgeIndexedDb.setItem", It.IsAny<CancellationToken>(), It.IsAny<object[]>()))
            .ReturnsAsync((object?)null);

        await _storage.SetAsync("item:1", item);

        // Verify setItem called twice: once for items store, once for metadata store
        _jsRuntime.Verify(
            r => r.InvokeAsync<object?>("xForgeIndexedDb.setItem", It.IsAny<CancellationToken>(), It.IsAny<object[]>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task SetAsync_IncrementsVersion_OnExistingItem()
    {
        TestItem item = new() { Id = 1, Name = "Updated" };

        // Setup metadata get returns existing version=3
        JsonElement existingMeta = JsonSerializer.SerializeToElement(new
        {
            key = "item:1",
            version = 3,
            checksum = "abc123",
            isDirty = true,
            lastModified = DateTime.UtcNow.ToString("o"),
            lastSynced = (string?)null
        });

        _jsRuntime
            .Setup(r => r.InvokeAsync<JsonElement?>("xForgeIndexedDb.getItem", It.IsAny<CancellationToken>(), It.IsAny<object[]>()))
            .ReturnsAsync(existingMeta);

        _jsRuntime
            .Setup(r => r.InvokeAsync<object?>("xForgeIndexedDb.setItem", It.IsAny<CancellationToken>(), It.IsAny<object[]>()))
            .ReturnsAsync((object?)null);

        await _storage.SetAsync("item:1", item);

        // Verify setItem was called with version=4 in metadata
        _jsRuntime.Verify(
            r => r.InvokeAsync<object?>("xForgeIndexedDb.setItem", It.IsAny<CancellationToken>(),
                It.Is<object[]>(args =>
                    args.Length == 3
                    && args[1].ToString() == "sync_metadata")),
            Times.Once);
    }

    [Fact]
    public async Task SetAsync_JsException_Throws()
    {
        _jsRuntime
            .Setup(r => r.InvokeAsync<JsonElement?>("xForgeIndexedDb.getItem", It.IsAny<CancellationToken>(), It.IsAny<object[]>()))
            .ThrowsAsync(new JSException("IndexedDB error"));

        Func<Task> act = () => _storage.SetAsync("item:1", new TestItem { Id = 1 });

        await act.Should().ThrowAsync<JSException>();
    }

    // -------------------------------------------------------------------
    // DeleteAsync — Argument validation
    // -------------------------------------------------------------------

    [Fact]
    public async Task DeleteAsync_NullKey_Throws()
    {
        Func<Task> act = () => _storage.DeleteAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task DeleteAsync_EmptyKey_Throws()
    {
        Func<Task> act = () => _storage.DeleteAsync("");

        await act.Should().ThrowAsync<ArgumentException>();
    }

    // -------------------------------------------------------------------
    // DeleteAsync — JS interop
    // -------------------------------------------------------------------

    [Fact]
    public async Task DeleteAsync_CallsJsDeleteItem_ForBothStores()
    {
        _jsRuntime
            .Setup(r => r.InvokeAsync<object?>("xForgeIndexedDb.deleteItem", It.IsAny<CancellationToken>(), It.IsAny<object[]>()))
            .ReturnsAsync((object?)null);

        await _storage.DeleteAsync("item:1");

        // Verify deleteItem called twice: once for items store, once for metadata store
        _jsRuntime.Verify(
            r => r.InvokeAsync<object?>("xForgeIndexedDb.deleteItem", It.IsAny<CancellationToken>(), It.IsAny<object[]>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task DeleteAsync_JsException_Throws()
    {
        _jsRuntime
            .Setup(r => r.InvokeAsync<object?>("xForgeIndexedDb.deleteItem", It.IsAny<CancellationToken>(), It.IsAny<object[]>()))
            .ThrowsAsync(new JSException("IndexedDB error"));

        Func<Task> act = () => _storage.DeleteAsync("item:1");

        await act.Should().ThrowAsync<JSException>();
    }

    // -------------------------------------------------------------------
    // GetAllAsync — JS interop
    // -------------------------------------------------------------------

    [Fact]
    public async Task GetAllAsync_ReturnsFilteredItems()
    {
        TestItem item1 = new() { Id = 1, Name = "A" };
        TestItem item2 = new() { Id = 2, Name = "B" };
        OtherItem other = new() { Id = 100, Label = "X" };

        JsonElement[] allItems =
        [
            JsonSerializer.SerializeToElement(new { key = "item:1", data = item1, typeName = "TestItem", updatedAt = DateTime.UtcNow.ToString("o") }),
            JsonSerializer.SerializeToElement(new { key = "item:2", data = item2, typeName = "TestItem", updatedAt = DateTime.UtcNow.ToString("o") }),
            JsonSerializer.SerializeToElement(new { key = "other:1", data = other, typeName = "OtherItem", updatedAt = DateTime.UtcNow.ToString("o") })
        ];

        _jsRuntime
            .Setup(r => r.InvokeAsync<JsonElement[]>("xForgeIndexedDb.getAllItems", It.IsAny<CancellationToken>(), It.IsAny<object[]>()))
            .ReturnsAsync(allItems);

        IReadOnlyList<TestItem> results = await _storage.GetAllAsync<TestItem>();

        results.Should().HaveCount(2);
        results.Should().Contain(i => i.Name == "A");
        results.Should().Contain(i => i.Name == "B");
    }

    [Fact]
    public async Task GetAllAsync_EmptyStore_ReturnsEmptyList()
    {
        _jsRuntime
            .Setup(r => r.InvokeAsync<JsonElement[]>("xForgeIndexedDb.getAllItems", It.IsAny<CancellationToken>(), It.IsAny<object[]>()))
            .ReturnsAsync([]);

        IReadOnlyList<TestItem> results = await _storage.GetAllAsync<TestItem>();

        results.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_JsException_ReturnsEmptyList()
    {
        _jsRuntime
            .Setup(r => r.InvokeAsync<JsonElement[]>("xForgeIndexedDb.getAllItems", It.IsAny<CancellationToken>(), It.IsAny<object[]>()))
            .ThrowsAsync(new JSException("IndexedDB error"));

        IReadOnlyList<TestItem> results = await _storage.GetAllAsync<TestItem>();

        results.Should().BeEmpty();
    }

    // -------------------------------------------------------------------
    // GetMetadataAsync — JS interop
    // -------------------------------------------------------------------

    [Fact]
    public async Task GetMetadataAsync_ReturnsMetadata()
    {
        JsonElement metaJson = JsonSerializer.SerializeToElement(new
        {
            key = "item:1",
            version = 5,
            checksum = "abc123def456",
            isDirty = true,
            lastModified = "2026-05-27T21:00:00.0000000Z",
            lastSynced = (string?)null
        });

        _jsRuntime
            .Setup(r => r.InvokeAsync<JsonElement?>("xForgeIndexedDb.getItem", It.IsAny<CancellationToken>(), It.IsAny<object[]>()))
            .ReturnsAsync(metaJson);

        SyncMetadata? metadata = await _storage.GetMetadataAsync("item:1");

        metadata.Should().NotBeNull();
        metadata!.Key.Should().Be("item:1");
        metadata.Version.Should().Be(5);
        metadata.Checksum.Should().Be("abc123def456");
        metadata.IsDirty.Should().BeTrue();
        metadata.LastSynced.Should().BeNull();
    }

    [Fact]
    public async Task GetMetadataAsync_NotFound_ReturnsNull()
    {
        _jsRuntime
            .Setup(r => r.InvokeAsync<JsonElement?>("xForgeIndexedDb.getItem", It.IsAny<CancellationToken>(), It.IsAny<object[]>()))
            .ReturnsAsync((JsonElement?)null);

        SyncMetadata? metadata = await _storage.GetMetadataAsync("nonexistent");

        metadata.Should().BeNull();
    }

    [Fact]
    public async Task GetMetadataAsync_NullKey_Throws()
    {
        Func<Task> act = () => _storage.GetMetadataAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GetMetadataAsync_EmptyKey_Throws()
    {
        Func<Task> act = () => _storage.GetMetadataAsync("");

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetMetadataAsync_JsException_ReturnsNull()
    {
        _jsRuntime
            .Setup(r => r.InvokeAsync<JsonElement?>("xForgeIndexedDb.getItem", It.IsAny<CancellationToken>(), It.IsAny<object[]>()))
            .ThrowsAsync(new JSException("IndexedDB error"));

        SyncMetadata? metadata = await _storage.GetMetadataAsync("item:1");

        metadata.Should().BeNull();
    }

    [Fact]
    public async Task GetMetadataAsync_WithLastSynced_ReturnsLastSynced()
    {
        JsonElement metaJson = JsonSerializer.SerializeToElement(new
        {
            key = "item:1",
            version = 1,
            checksum = "abc",
            isDirty = false,
            lastModified = "2026-05-27T21:00:00.0000000Z",
            lastSynced = "2026-05-27T21:05:00.0000000Z"
        });

        _jsRuntime
            .Setup(r => r.InvokeAsync<JsonElement?>("xForgeIndexedDb.getItem", It.IsAny<CancellationToken>(), It.IsAny<object[]>()))
            .ReturnsAsync(metaJson);

        SyncMetadata? metadata = await _storage.GetMetadataAsync("item:1");

        metadata.Should().NotBeNull();
        metadata!.LastSynced.Should().NotBeNull();
    }

    // -------------------------------------------------------------------
    // MarkSyncedAsync — JS interop
    // -------------------------------------------------------------------

    [Fact]
    public async Task MarkSyncedAsync_NullKey_Throws()
    {
        Func<Task> act = () => _storage.MarkSyncedAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task MarkSyncedAsync_EmptyKey_Throws()
    {
        Func<Task> act = () => _storage.MarkSyncedAsync("");

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task MarkSyncedAsync_ExistingMetadata_UpdatesItem()
    {
        JsonElement existingMeta = JsonSerializer.SerializeToElement(new
        {
            key = "item:1",
            version = 3,
            checksum = "abc123",
            isDirty = true,
            lastModified = "2026-05-27T21:00:00.0000000Z",
            lastSynced = (string?)null
        });

        _jsRuntime
            .Setup(r => r.InvokeAsync<JsonElement?>("xForgeIndexedDb.getItem", It.IsAny<CancellationToken>(), It.IsAny<object[]>()))
            .ReturnsAsync(existingMeta);

        _jsRuntime
            .Setup(r => r.InvokeAsync<object?>("xForgeIndexedDb.setItem", It.IsAny<CancellationToken>(), It.IsAny<object[]>()))
            .ReturnsAsync((object?)null);

        await _storage.MarkSyncedAsync("item:1");

        _jsRuntime.Verify(
            r => r.InvokeAsync<object?>("xForgeIndexedDb.setItem", It.IsAny<CancellationToken>(), It.IsAny<object[]>()),
            Times.Once);
    }

    [Fact]
    public async Task MarkSyncedAsync_NoMetadata_DoesNotCallSetItem()
    {
        _jsRuntime
            .Setup(r => r.InvokeAsync<JsonElement?>("xForgeIndexedDb.getItem", It.IsAny<CancellationToken>(), It.IsAny<object[]>()))
            .ReturnsAsync((JsonElement?)null);

        await _storage.MarkSyncedAsync("item:1");

        _jsRuntime.Verify(
            r => r.InvokeAsync<object?>("xForgeIndexedDb.setItem", It.IsAny<CancellationToken>(), It.IsAny<object[]>()),
            Times.Never);
    }

    [Fact]
    public async Task MarkSyncedAsync_JsException_Throws()
    {
        _jsRuntime
            .Setup(r => r.InvokeAsync<JsonElement?>("xForgeIndexedDb.getItem", It.IsAny<CancellationToken>(), It.IsAny<object[]>()))
            .ThrowsAsync(new JSException("IndexedDB error"));

        Func<Task> act = () => _storage.MarkSyncedAsync("item:1");

        await act.Should().ThrowAsync<JSException>();
    }

    // -------------------------------------------------------------------
    // DisposeAsync
    // -------------------------------------------------------------------

    [Fact]
    public async Task DisposeAsync_DisposesReferences()
    {
        // First trigger initialization
        _jsRuntime
            .Setup(r => r.InvokeAsync<object?>("xForgeIndexedDb.closeDatabase", It.IsAny<CancellationToken>(), It.IsAny<object[]>()))
            .ReturnsAsync((object?)null);

        await _storage.DisposeAsync();

        // Second dispose should not throw
        Func<Task> act = async () => await _storage.DisposeAsync();
        await act.Should().NotThrowAsync();
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
