namespace XForge.Sync.Storage;

/// <summary>
/// Abstracts local storage for offline-first data persistence.
/// Implementations can target SQLite, IndexedDB, or other storage backends.
/// </summary>
public interface ILocalStorage
{
    /// <summary>
    /// Gets an item by key.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="key">The unique key.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The item, or default if not found.</returns>
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);

    /// <summary>
    /// Stores an item with the specified key.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="key">The unique key.</param>
    /// <param name="value">The value to store.</param>
    /// <param name="ct">Cancellation token.</param>
    Task SetAsync<T>(string key, T value, CancellationToken ct = default);

    /// <summary>
    /// Deletes an item by key.
    /// </summary>
    /// <param name="key">The unique key.</param>
    /// <param name="ct">Cancellation token.</param>
    Task DeleteAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// Gets all items of the specified type.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>All stored items of the type.</returns>
    Task<IReadOnlyList<T>> GetAllAsync<T>(CancellationToken ct = default);

    /// <summary>
    /// Gets the sync metadata for an item.
    /// </summary>
    /// <param name="key">The unique key.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The metadata, or null if the item doesn't exist.</returns>
    Task<SyncMetadata?> GetMetadataAsync(string key, CancellationToken ct = default);
}
