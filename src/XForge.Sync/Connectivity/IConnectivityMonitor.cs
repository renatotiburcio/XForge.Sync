namespace XForge.Sync.Connectivity;

/// <summary>
/// Monitors network connectivity status and notifies when it changes.
/// </summary>
public interface IConnectivityMonitor
{
    /// <summary>
    /// Gets a value indicating whether the device is currently online.
    /// </summary>
    bool IsOnline { get; }

    /// <summary>
    /// Event raised when connectivity status changes.
    /// </summary>
    event Action<bool>? ConnectivityChanged;

    /// <summary>
    /// Actively checks connectivity (e.g., ping the server).
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if online; otherwise, false.</returns>
    Task<bool> CheckConnectivityAsync(CancellationToken ct = default);
}
