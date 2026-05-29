namespace XForge.Sync.Connectivity;

/// <summary>
/// Default implementation of <see cref="IConnectivityMonitor"/>.
/// Tracks online/offline status and allows manual status updates.
/// </summary>
public sealed class ConnectivityMonitor : IConnectivityMonitor
{
    private bool _isOnline = true;
#if NET9_0_OR_GREATER
    private readonly Lock _lock = new();
#else
    private readonly object _lock = new();
#endif

    /// <inheritdoc />
    public bool IsOnline
    {
        get
        {
            lock (_lock)
            {
                return _isOnline;
            }
        }
    }

    /// <inheritdoc />
    public event Action<bool>? ConnectivityChanged;

    /// <inheritdoc />
    public Task<bool> CheckConnectivityAsync(CancellationToken ct = default)
    {
        return Task.FromResult(IsOnline);
    }

    /// <summary>
    /// Sets the connectivity status and raises the <see cref="ConnectivityChanged"/> event if changed.
    /// This method is intended for use by platform-specific connectivity providers.
    /// </summary>
    /// <param name="isOnline">The new connectivity status.</param>
    public void SetOnlineStatus(bool isOnline)
    {
        bool changed;

        lock (_lock)
        {
            changed = _isOnline != isOnline;
            _isOnline = isOnline;
        }

        if (changed)
        {
            ConnectivityChanged?.Invoke(isOnline);
        }
    }
}
