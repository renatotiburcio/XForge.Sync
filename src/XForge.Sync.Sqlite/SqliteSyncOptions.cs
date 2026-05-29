namespace XForge.Sync.Sqlite;

/// <summary>
/// Configuration options for <see cref="SqliteLocalStorage"/> and <see cref="SqliteChangeTracker"/>.
/// </summary>
public sealed class SqliteSyncOptions
{
    /// <summary>
    /// The configuration section name.
    /// </summary>
    public const string SectionName = "XForge:Sync:Sqlite";

    /// <summary>
    /// Gets or sets the SQLite connection string.
    /// Default is "Data Source=xforge_sync.db".
    /// </summary>
    public string ConnectionString { get; set; } = "Data Source=xforge_sync.db";

    /// <summary>
    /// Gets or sets the table name for local storage items. Default is "xf_sync_items".
    /// </summary>
    public string ItemsTableName { get; set; } = "xf_sync_items";

    /// <summary>
    /// Gets or sets the table name for sync metadata. Default is "xf_sync_metadata".
    /// </summary>
    public string MetadataTableName { get; set; } = "xf_sync_metadata";

    /// <summary>
    /// Gets or sets the table name for tracked changes. Default is "xf_sync_changes".
    /// </summary>
    public string ChangesTableName { get; set; } = "xf_sync_changes";

    /// <summary>
    /// Gets or sets whether to automatically create tables on startup. Default is true.
    /// </summary>
    public bool AutoCreateTables { get; set; } = true;
}
