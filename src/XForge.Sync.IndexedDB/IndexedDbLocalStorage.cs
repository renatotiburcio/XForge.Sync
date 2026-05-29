using Microsoft.JSInterop;
using XForge.Sync.Storage;

namespace XForge.Sync.IndexedDB;

/// <summary>
/// IndexedDB implementation of <see cref="ILocalStorage"/> for Blazor WASM offline-first persistence.
/// Uses JS interop to access the browser's IndexedDB API.
/// </summary>
public sealed class IndexedDbLocalStorage : ILocalStorage, IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private readonly IndexedDbOptions _options;
    private readonly ILogger<IndexedDbLocalStorage> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private IJSObjectReference? _module;
    private IJSObjectReference? _db;
    private bool _initialized;

    /// <summary>
    /// Creates a new <see cref="IndexedDbLocalStorage"/>.
    /// </summary>
    /// <param name="jsRuntime">The JS runtime for browser interop.</param>
    /// <param name="options">The configuration options.</param>
    /// <param name="logger">The logger.</param>
    public IndexedDbLocalStorage(
        IJSRuntime jsRuntime,
        IOptions<IndexedDbOptions> options,
        ILogger<IndexedDbLocalStorage> logger)
    {
        _jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));
        _options = (options ?? throw new ArgumentNullException(nameof(options))).Value;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false
        };
    }

    /// <inheritdoc />
    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        await EnsureInitializedAsync(ct);

        try
        {
            JsonElement? result = await _jsRuntime.InvokeAsync<JsonElement?>(
                "xForgeIndexedDb.getItem", ct, [_db, _options.ItemsStoreName, key]);

            if (result is null)
            {
                _logger.LogDebug("Item not found for key '{Key}'", key);
                return default;
            }

            // The stored value has a 'data' property containing the JSON-serialized entity
            if (result.Value.TryGetProperty("data", out JsonElement dataElement))
            {
                T? item = dataElement.Deserialize<T>(_jsonOptions);
                _logger.LogDebug("Retrieved item for key '{Key}'", key);
                return item;
            }

            _logger.LogWarning("Item for key '{Key}' has no 'data' property", key);
            return default;
        }
        catch (JSException ex)
        {
            _logger.LogError(ex, "JS error retrieving item for key '{Key}'", key);
            return default;
        }
    }

    /// <inheritdoc />
    public async Task SetAsync<T>(string key, T value, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentNullException.ThrowIfNull(value);
        await EnsureInitializedAsync(ct);

        try
        {
            string dataJson = JsonSerializer.Serialize(value, _jsonOptions);
            DateTime now = DateTime.UtcNow;
            string checksum = ComputeChecksum(dataJson);

            // Get current version for metadata
            long version = 1;
            JsonElement? existingMeta = await _jsRuntime.InvokeAsync<JsonElement?>(
                "xForgeIndexedDb.getItem", ct, [_db, _options.MetadataStoreName, key]);

            if (existingMeta.HasValue
                && existingMeta.Value.TryGetProperty("version", out JsonElement versionElement)
                && versionElement.ValueKind == JsonValueKind.Number)
            {
                version = versionElement.GetInt64() + 1;
            }

            // Store the item
            var itemRecord = new { key, data = value, typeName = typeof(T).Name, updatedAt = now.ToString("o") };
            await _jsRuntime.InvokeAsync<object?>(
                "xForgeIndexedDb.setItem", ct, [_db, _options.ItemsStoreName, itemRecord]);

            // Store the metadata
            var metadataRecord = new
            {
                key,
                version,
                checksum,
                isDirty = true,
                lastModified = now.ToString("o"),
                lastSynced = (string?)null
            };
            await _jsRuntime.InvokeAsync<object?>(
                "xForgeIndexedDb.setItem", ct, [_db, _options.MetadataStoreName, metadataRecord]);

            _logger.LogDebug("Stored item with key '{Key}', version {Version}", key, version);
        }
        catch (JSException ex)
        {
            _logger.LogError(ex, "JS error storing item for key '{Key}'", key);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task DeleteAsync(string key, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        await EnsureInitializedAsync(ct);

        try
        {
            await _jsRuntime.InvokeAsync<object?>(
                "xForgeIndexedDb.deleteItem", ct, [_db, _options.ItemsStoreName, key]);
            await _jsRuntime.InvokeAsync<object?>(
                "xForgeIndexedDb.deleteItem", ct, [_db, _options.MetadataStoreName, key]);

            _logger.LogDebug("Deleted item with key '{Key}'", key);
        }
        catch (JSException ex)
        {
            _logger.LogError(ex, "JS error deleting item for key '{Key}'", key);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<T>> GetAllAsync<T>(CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct);

        try
        {
            JsonElement[] results = await _jsRuntime.InvokeAsync<JsonElement[]>(
                "xForgeIndexedDb.getAllItems", ct, [_db, _options.ItemsStoreName]);

            List<T> items = [];
            string typeName = typeof(T).Name;

            foreach (JsonElement record in results)
            {
                if (record.TryGetProperty("typeName", out JsonElement typeNameElement)
                    && typeNameElement.GetString() == typeName
                    && record.TryGetProperty("data", out JsonElement dataElement))
                {
                    T? item = dataElement.Deserialize<T>(_jsonOptions);
                    if (item is not null)
                    {
                        items.Add(item);
                    }
                }
            }

            _logger.LogDebug("Retrieved {Count} items of type '{Type}'", items.Count, typeof(T).Name);
            return items;
        }
        catch (JSException ex)
        {
            _logger.LogError(ex, "JS error retrieving all items of type '{Type}'", typeof(T).Name);
            return [];
        }
    }

    /// <inheritdoc />
    public async Task<SyncMetadata?> GetMetadataAsync(string key, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        await EnsureInitializedAsync(ct);

        try
        {
            JsonElement? result = await _jsRuntime.InvokeAsync<JsonElement?>(
                "xForgeIndexedDb.getItem", ct, [_db, _options.MetadataStoreName, key]);

            if (result is null)
            {
                return null;
            }

            JsonElement meta = result.Value;
            return new SyncMetadata
            {
                Key = meta.GetProperty("key").GetString()!,
                Version = meta.GetProperty("version").GetInt64(),
                Checksum = meta.TryGetProperty("checksum", out JsonElement cs) && cs.ValueKind == JsonValueKind.String
                    ? cs.GetString()
                    : null,
                IsDirty = meta.TryGetProperty("isDirty", out JsonElement dirty) && dirty.GetBoolean(),
                LastModified = DateTime.Parse(
                    meta.GetProperty("lastModified").GetString()!,
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.RoundtripKind),
                LastSynced = meta.TryGetProperty("lastSynced", out JsonElement ls) && ls.ValueKind == JsonValueKind.String
                    ? DateTime.Parse(ls.GetString()!, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind)
                    : null
            };
        }
        catch (JSException ex)
        {
            _logger.LogError(ex, "JS error retrieving metadata for key '{Key}'", key);
            return null;
        }
    }

    /// <summary>
    /// Marks the specified key as synced (clears dirty flag and sets last synced timestamp).
    /// </summary>
    /// <param name="key">The key to mark as synced.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task MarkSyncedAsync(string key, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        await EnsureInitializedAsync(ct);

        try
        {
            JsonElement? existing = await _jsRuntime.InvokeAsync<JsonElement?>(
                "xForgeIndexedDb.getItem", ct, [_db, _options.MetadataStoreName, key]);

            if (existing is null)
            {
                _logger.LogWarning("Cannot mark key '{Key}' as synced — metadata not found", key);
                return;
            }

            JsonElement meta = existing.Value;
            long version = meta.GetProperty("version").GetInt64();
            string? checksum = meta.TryGetProperty("checksum", out JsonElement cs) && cs.ValueKind == JsonValueKind.String
                ? cs.GetString()
                : null;
            string lastModified = meta.GetProperty("lastModified").GetString()!;

            var updatedMeta = new
            {
                key,
                version,
                checksum,
                isDirty = false,
                lastModified,
                lastSynced = DateTime.UtcNow.ToString("o")
            };

            await _jsRuntime.InvokeAsync<object?>(
                "xForgeIndexedDb.setItem", ct, [_db, _options.MetadataStoreName, updatedMeta]);

            _logger.LogDebug("Marked key '{Key}' as synced", key);
        }
        catch (JSException ex)
        {
            _logger.LogError(ex, "JS error marking key '{Key}' as synced", key);
            throw;
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_db is not null)
        {
            try
            {
                await _jsRuntime.InvokeAsync<object?>("xForgeIndexedDb.closeDatabase", default, [_db]);
            }
            catch (JSDisconnectedException)
            {
                // Circuit already disconnected — ignore
            }

            await _db.DisposeAsync();
            _db = null;
        }

        if (_module is not null)
        {
            await _module.DisposeAsync();
            _module = null;
        }
    }

    private async Task EnsureInitializedAsync(CancellationToken ct)
    {
        if (_initialized)
        {
            return;
        }

        _module = await _jsRuntime.InvokeAsync<IJSObjectReference>(
            "import", ct, ["./_content/XForge.Sync.IndexedDB/xforge-sync-indexeddb.js"]);

        _db = await _jsRuntime.InvokeAsync<IJSObjectReference>(
            "xForgeIndexedDb.openDatabase", ct,
            [ _options.DatabaseName, _options.Version,
            _options.ItemsStoreName, _options.MetadataStoreName ]);

        _initialized = true;
        _logger.LogDebug("Initialized IndexedDB database '{DatabaseName}' (v{Version})",
            _options.DatabaseName, _options.Version);
    }

    private static string ComputeChecksum(string data)
    {
        byte[] hash = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash)[..16]; // Truncate for storage efficiency
    }
}
