using Microsoft.Data.Sqlite;
using XForge.Sync.Storage;

namespace XForge.Sync.Sqlite;

/// <summary>
/// SQLite implementation of <see cref="ILocalStorage"/> for desktop and mobile offline-first persistence.
/// Stores entities as JSON blobs with associated sync metadata.
/// </summary>
public sealed class SqliteLocalStorage : ILocalStorage, IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly SqliteSyncOptions _options;
    private readonly ILogger<SqliteLocalStorage> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly bool _ownsConnection;

    /// <summary>
    /// Creates a new <see cref="SqliteLocalStorage"/>.
    /// </summary>
    /// <param name="options">The configuration options.</param>
    /// <param name="logger">The logger.</param>
    public SqliteLocalStorage(
        IOptions<SqliteSyncOptions> options,
        ILogger<SqliteLocalStorage> logger)
    {
        _options = (options ?? throw new ArgumentNullException(nameof(options))).Value;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false
        };

        _connection = new SqliteConnection(_options.ConnectionString);
        _connection.Open();
        _ownsConnection = true;

        if (_options.AutoCreateTables)
        {
            EnsureTables();
        }
    }

    /// <summary>
    /// Creates a new <see cref="SqliteLocalStorage"/> with an externally managed connection.
    /// </summary>
    /// <param name="connection">The SQLite connection (caller manages lifetime).</param>
    /// <param name="options">The configuration options.</param>
    /// <param name="logger">The logger.</param>
    public SqliteLocalStorage(
        SqliteConnection connection,
        IOptions<SqliteSyncOptions> options,
        ILogger<SqliteLocalStorage> logger)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _options = (options ?? throw new ArgumentNullException(nameof(options))).Value;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false
        };

        _ownsConnection = false;

        if (_options.AutoCreateTables)
        {
            EnsureTables();
        }
    }

    /// <inheritdoc />
    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);

        await using SqliteCommand cmd = _connection.CreateCommand();
        cmd.CommandText = $"""
            SELECT data FROM [{_options.ItemsTableName}]
            WHERE [key] = @key
            """;
        cmd.Parameters.AddWithValue("@key", key);

        object? result = await cmd.ExecuteScalarAsync(ct);

        if (result is not string json)
        {
            _logger.LogDebug("Item not found for key '{Key}'", key);
            return default;
        }

        T? item = JsonSerializer.Deserialize<T>(json, _jsonOptions);
        _logger.LogDebug("Retrieved item for key '{Key}'", key);
        return item;
    }

    /// <inheritdoc />
    public async Task SetAsync<T>(string key, T value, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentNullException.ThrowIfNull(value);

        string json = JsonSerializer.Serialize(value, _jsonOptions);
        DateTime now = DateTime.UtcNow;
        string checksum = ComputeChecksum(json);

        await using SqliteTransaction transaction = _connection.BeginTransaction();

        try
        {
            // Upsert item
            await using (SqliteCommand cmd = _connection.CreateCommand())
            {
                cmd.Transaction = transaction;
                cmd.CommandText = $"""
                    INSERT INTO [{_options.ItemsTableName}] ([key], [data], [type_name], [updated_at])
                    VALUES (@key, @data, @typeName, @updatedAt)
                    ON CONFLICT([key]) DO UPDATE SET
                        [data] = @data,
                        [type_name] = @typeName,
                        [updated_at] = @updatedAt
                    """;
                cmd.Parameters.AddWithValue("@key", key);
                cmd.Parameters.AddWithValue("@data", json);
                cmd.Parameters.AddWithValue("@typeName", typeof(T).Name);
                cmd.Parameters.AddWithValue("@updatedAt", now.ToString("o"));
                await cmd.ExecuteNonQueryAsync(ct);
            }

            // Upsert metadata
            long version = await GetNextVersionAsync(key, transaction, ct);

            await using (SqliteCommand cmd = _connection.CreateCommand())
            {
                cmd.Transaction = transaction;
                cmd.CommandText = $"""
                    INSERT INTO [{_options.MetadataTableName}] ([key], [version], [checksum], [is_dirty], [last_modified])
                    VALUES (@key, @version, @checksum, 1, @lastModified)
                    ON CONFLICT([key]) DO UPDATE SET
                        [version] = @version,
                        [checksum] = @checksum,
                        [is_dirty] = 1,
                        [last_modified] = @lastModified
                    """;
                cmd.Parameters.AddWithValue("@key", key);
                cmd.Parameters.AddWithValue("@version", version);
                cmd.Parameters.AddWithValue("@checksum", checksum);
                cmd.Parameters.AddWithValue("@lastModified", now.ToString("o"));
                await cmd.ExecuteNonQueryAsync(ct);
            }

            await transaction.CommitAsync(ct);
            _logger.LogDebug("Stored item with key '{Key}', version {Version}", key, version);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task DeleteAsync(string key, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);

        await using SqliteCommand cmd = _connection.CreateCommand();
        cmd.CommandText = $"""
            DELETE FROM [{_options.ItemsTableName}] WHERE [key] = @key;
            DELETE FROM [{_options.MetadataTableName}] WHERE [key] = @key;
            """;
        cmd.Parameters.AddWithValue("@key", key);

        int affected = await cmd.ExecuteNonQueryAsync(ct);
        _logger.LogDebug("Deleted item with key '{Key}' (rows affected: {Affected})", key, affected);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<T>> GetAllAsync<T>(CancellationToken ct = default)
    {
        List<T> items = [];

        await using SqliteCommand cmd = _connection.CreateCommand();
        cmd.CommandText = $"""
            SELECT [data] FROM [{_options.ItemsTableName}]
            WHERE [type_name] = @typeName
            ORDER BY [updated_at]
            """;
        cmd.Parameters.AddWithValue("@typeName", typeof(T).Name);

        await using SqliteDataReader reader = await cmd.ExecuteReaderAsync(ct);

        while (await reader.ReadAsync(ct))
        {
            string json = reader.GetString(0);
            T? item = JsonSerializer.Deserialize<T>(json, _jsonOptions);
            if (item is not null)
            {
                items.Add(item);
            }
        }

        _logger.LogDebug("Retrieved {Count} items of type '{Type}'", items.Count, typeof(T).Name);
        return items;
    }

    /// <inheritdoc />
    public async Task<SyncMetadata?> GetMetadataAsync(string key, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);

        await using SqliteCommand cmd = _connection.CreateCommand();
        cmd.CommandText = $"""
            SELECT [key], [version], [checksum], [is_dirty], [last_modified], [last_synced]
            FROM [{_options.MetadataTableName}]
            WHERE [key] = @key
            """;
        cmd.Parameters.AddWithValue("@key", key);

        await using SqliteDataReader reader = await cmd.ExecuteReaderAsync(ct);

        if (!await reader.ReadAsync(ct))
        {
            return null;
        }

        SyncMetadata metadata = new()
        {
            Key = reader.GetString(0),
            Version = reader.GetInt64(1),
            Checksum = reader.IsDBNull(2) ? null : reader.GetString(2),
            IsDirty = reader.GetInt32(3) == 1,
            LastModified = DateTime.Parse(reader.GetString(4), System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind),
            LastSynced = reader.IsDBNull(5) ? null : DateTime.Parse(reader.GetString(5), System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind)
        };

        return metadata;
    }

    /// <summary>
    /// Marks the specified key as synced (clears dirty flag and sets last synced timestamp).
    /// </summary>
    /// <param name="key">The key to mark as synced.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task MarkSyncedAsync(string key, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);

        await using SqliteCommand cmd = _connection.CreateCommand();
        cmd.CommandText = $"""
            UPDATE [{_options.MetadataTableName}]
            SET [is_dirty] = 0, [last_synced] = @lastSynced
            WHERE [key] = @key
            """;
        cmd.Parameters.AddWithValue("@key", key);
        cmd.Parameters.AddWithValue("@lastSynced", DateTime.UtcNow.ToString("o"));

        await cmd.ExecuteNonQueryAsync(ct);
        _logger.LogDebug("Marked key '{Key}' as synced", key);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_ownsConnection)
        {
            _connection.Dispose();
        }
    }

    private void EnsureTables()
    {
        using SqliteCommand cmd = _connection.CreateCommand();
        cmd.CommandText = $"""
            CREATE TABLE IF NOT EXISTS [{_options.ItemsTableName}] (
                [key] TEXT NOT NULL PRIMARY KEY,
                [data] TEXT NOT NULL,
                [type_name] TEXT NOT NULL,
                [updated_at] TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS [{_options.MetadataTableName}] (
                [key] TEXT NOT NULL PRIMARY KEY,
                [version] INTEGER NOT NULL DEFAULT 1,
                [checksum] TEXT,
                [is_dirty] INTEGER NOT NULL DEFAULT 1,
                [last_modified] TEXT NOT NULL,
                [last_synced] TEXT,
                FOREIGN KEY ([key]) REFERENCES [{_options.ItemsTableName}]([key]) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS [{_options.ChangesTableName}] (
                [id] INTEGER PRIMARY KEY AUTOINCREMENT,
                [key] TEXT NOT NULL,
                [change_type] INTEGER NOT NULL,
                [entity_type] TEXT NOT NULL,
                [data] TEXT,
                [version] INTEGER NOT NULL,
                [tracked_at] TEXT NOT NULL,
                [is_synced] INTEGER NOT NULL DEFAULT 0
            );

            CREATE INDEX IF NOT EXISTS [ix_{_options.ChangesTableName}_synced]
                ON [{_options.ChangesTableName}] ([is_synced]);

            CREATE INDEX IF NOT EXISTS [ix_{_options.ChangesTableName}_key]
                ON [{_options.ChangesTableName}] ([key]);
            """;
        cmd.ExecuteNonQuery();

        _logger.LogDebug("Ensured sync tables exist: {Items}, {Metadata}, {Changes}",
            _options.ItemsTableName, _options.MetadataTableName, _options.ChangesTableName);
    }

    private async Task<long> GetNextVersionAsync(string key, SqliteTransaction transaction, CancellationToken ct)
    {
        await using SqliteCommand cmd = _connection.CreateCommand();
        cmd.Transaction = transaction;
        cmd.CommandText = $"""
            SELECT COALESCE([version], 0) FROM [{_options.MetadataTableName}]
            WHERE [key] = @key
            """;
        cmd.Parameters.AddWithValue("@key", key);

        object? result = await cmd.ExecuteScalarAsync(ct);
        return result is long v ? v + 1 : 1;
    }

    private static string ComputeChecksum(string data)
    {
        byte[] hash = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash)[..16]; // Truncate for storage efficiency
    }
}
