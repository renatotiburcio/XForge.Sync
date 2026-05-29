namespace XForge.Sync.IndexedDB;

/// <summary>
/// Configuration options for <see cref="IndexedDbLocalStorage"/>.
/// </summary>
public sealed class IndexedDbOptions
{
    /// <summary>
    /// The configuration section name.
    /// </summary>
    public const string SectionName = "XForge:Sync:IndexedDB";

    /// <summary>
    /// Gets or sets the IndexedDB database name. Default is "xforge_sync".
    /// </summary>
    public string DatabaseName { get; set; } = "xforge_sync";

    /// <summary>
    /// Gets or sets the database version for schema upgrades. Default is 1.
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// Gets or sets the object store name for local storage items. Default is "sync_items".
    /// </summary>
    public string ItemsStoreName { get; set; } = "sync_items";

    /// <summary>
    /// Gets or sets the object store name for sync metadata. Default is "sync_metadata".
    /// </summary>
    public string MetadataStoreName { get; set; } = "sync_metadata";
}
