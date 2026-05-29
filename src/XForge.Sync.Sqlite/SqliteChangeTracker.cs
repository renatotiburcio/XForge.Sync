using Microsoft.Data.Sqlite;
using XForge.Sync.Tracking;

namespace XForge.Sync.Sqlite;

/// <summary>
/// SQLite implementation of <see cref="IChangeTracker"/>.
/// Persists tracked changes to a SQLite database for reliable offline-first sync.
/// </summary>
public sealed class SqliteChangeTracker : IChangeTracker, IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly SqliteSyncOptions _options;
    private readonly ILogger<SqliteChangeTracker> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly bool _ownsConnection;

    /// <summary>
    /// Creates a new <see cref="SqliteChangeTracker"/>.
    /// </summary>
    /// <param name="options">The configuration options.</param>
    /// <param name="logger">The logger.</param>
    public SqliteChangeTracker(
        IOptions<SqliteSyncOptions> options,
        ILogger<SqliteChangeTracker> logger)
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
            EnsureTable();
        }
    }

    /// <summary>
    /// Creates a new <see cref="SqliteChangeTracker"/> with an externally managed connection.
    /// </summary>
    /// <param name="connection">The SQLite connection (caller manages lifetime).</param>
    /// <param name="options">The configuration options.</param>
    /// <param name="logger">The logger.</param>
    public SqliteChangeTracker(
        SqliteConnection connection,
        IOptions<SqliteSyncOptions> options,
        ILogger<SqliteChangeTracker> logger)
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
            EnsureTable();
        }
    }

    /// <inheritdoc />
    public async Task TrackChangeAsync<T>(T entity, ChangeType changeType, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        string entityType = typeof(T).Name;
        string key = $"{entityType}:{GetEntityKey(entity)}";
        string data = JsonSerializer.Serialize(entity, _jsonOptions);
        long version = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        DateTime now = DateTime.UtcNow;

        await using SqliteCommand cmd = _connection.CreateCommand();
        cmd.CommandText = $"""
            INSERT INTO [{_options.ChangesTableName}]
                ([key], [change_type], [entity_type], [data], [version], [tracked_at], [is_synced])
            VALUES (@key, @changeType, @entityType, @data, @version, @trackedAt, 0)
            """;
        cmd.Parameters.AddWithValue("@key", key);
        cmd.Parameters.AddWithValue("@changeType", (int)changeType);
        cmd.Parameters.AddWithValue("@entityType", entityType);
        cmd.Parameters.AddWithValue("@data", data);
        cmd.Parameters.AddWithValue("@version", version);
        cmd.Parameters.AddWithValue("@trackedAt", now.ToString("o"));

        await cmd.ExecuteNonQueryAsync(ct);
        _logger.LogDebug("Tracked {ChangeType} for entity '{Key}' (version {Version})", changeType, key, version);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TrackedChange>> GetPendingChangesAsync(CancellationToken ct = default)
    {
        List<TrackedChange> changes = [];

        await using SqliteCommand cmd = _connection.CreateCommand();
        cmd.CommandText = $"""
            SELECT [key], [change_type], [entity_type], [data], [version], [tracked_at]
            FROM [{_options.ChangesTableName}]
            WHERE [is_synced] = 0
            ORDER BY [tracked_at]
            """;

        await using SqliteDataReader reader = await cmd.ExecuteReaderAsync(ct);

        while (await reader.ReadAsync(ct))
        {
            TrackedChange change = new()
            {
                Key = reader.GetString(0),
                ChangeType = (ChangeType)reader.GetInt32(1),
                EntityType = reader.GetString(2),
                Data = reader.IsDBNull(3) ? null : reader.GetString(3),
                Version = reader.GetInt64(4),
                TrackedAt = DateTime.Parse(reader.GetString(5), System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind)
            };
            changes.Add(change);
        }

        _logger.LogDebug("Found {Count} pending changes", changes.Count);
        return changes;
    }

    /// <inheritdoc />
    public async Task MarkSyncedAsync(IEnumerable<TrackedChange> changes, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(changes);

        await using SqliteTransaction transaction = _connection.BeginTransaction();

        try
        {
            foreach (TrackedChange change in changes)
            {
                await using SqliteCommand cmd = _connection.CreateCommand();
                cmd.Transaction = transaction;
                cmd.CommandText = $"""
                    UPDATE [{_options.ChangesTableName}]
                    SET [is_synced] = 1
                    WHERE [key] = @key AND [version] = @version AND [is_synced] = 0
                    """;
                cmd.Parameters.AddWithValue("@key", change.Key);
                cmd.Parameters.AddWithValue("@version", change.Version);
                await cmd.ExecuteNonQueryAsync(ct);
            }

            await transaction.CommitAsync(ct);
            _logger.LogDebug("Marked {Count} changes as synced", changes.Count());
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task ClearAsync(CancellationToken ct = default)
    {
        await using SqliteCommand cmd = _connection.CreateCommand();
        cmd.CommandText = $"DELETE FROM [{_options.ChangesTableName}]";

        int deleted = await cmd.ExecuteNonQueryAsync(ct);
        _logger.LogDebug("Cleared {Count} tracked changes", deleted);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_ownsConnection)
        {
            _connection.Dispose();
        }
    }

    private void EnsureTable()
    {
        using SqliteCommand cmd = _connection.CreateCommand();
        cmd.CommandText = $"""
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
    }

    private static string GetEntityKey<T>(T entity)
    {
        System.Reflection.PropertyInfo? idProp = typeof(T).GetProperty("Id");
        if (idProp is not null)
        {
            object? value = idProp.GetValue(entity);
            return value?.ToString() ?? Guid.NewGuid().ToString();
        }

        return entity!.GetHashCode().ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}
