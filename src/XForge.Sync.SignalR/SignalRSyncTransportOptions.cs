namespace XForge.Sync.SignalR;

/// <summary>
/// Configuration options for <see cref="SignalRSyncTransport"/>.
/// </summary>
public sealed class SignalRSyncTransportOptions
{
    /// <summary>
    /// The configuration section name.
    /// </summary>
    public const string SectionName = "XForge:Sync:SignalR";

    /// <summary>
    /// Gets or sets the SignalR hub URL (e.g., "https://api.example.com/hubs/sync").
    /// </summary>
    public required string HubUrl { get; set; }

    /// <summary>
    /// Gets or sets the hub method name for sending sync requests. Default is "Sync".
    /// </summary>
    public string SyncMethodName { get; set; } = "Sync";

    /// <summary>
    /// Gets or sets the hub method name for health checks. Default is "Ping".
    /// </summary>
    public string PingMethodName { get; set; } = "Ping";

    /// <summary>
    /// Gets or sets the SignalR transport type. Default is Automatic (tries WebSockets first, falls back to SSE/LongPolling).
    /// </summary>
    public HttpTransportType TransportType { get; set; } = HttpTransportType.None;

    /// <summary>
    /// Gets or sets the interval at which the client sends keep-alive pings. Default is 15 seconds.
    /// </summary>
    public TimeSpan KeepAliveInterval { get; set; } = TimeSpan.FromSeconds(15);

    /// <summary>
    /// Gets or sets the timeout for server pings. Default is 30 seconds.
    /// If the server doesn't respond within this period, the connection is considered lost.
    /// </summary>
    public TimeSpan ServerTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the maximum number of retry attempts for connection failures. Default is 3.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Gets or sets the base delay for exponential backoff between retries. Default is 1 second.
    /// </summary>
    public TimeSpan RetryBaseDelay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Gets or sets the maximum time to wait for a hub method response. Default is 30 seconds.
    /// </summary>
    public TimeSpan InvokeTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the access token factory for authentication. Called each time a connection is established.
    /// </summary>
    public Func<CancellationToken, Task<string?>>? AccessTokenProvider { get; set; }

    /// <summary>
    /// Gets or sets custom HTTP headers to include in the SignalR connection request.
    /// </summary>
    public Dictionary<string, string> DefaultHeaders { get; set; } = [];

    /// <summary>
    /// Gets or sets whether to automatically reconnect when the connection is lost. Default is true.
    /// </summary>
    public bool AutomaticReconnect { get; set; } = true;

    /// <summary>
    /// Gets or sets the retry delays for automatic reconnect. Default is 0s, 2s, 10s, 30s.
    /// Only used when <see cref="AutomaticReconnect"/> is true.
    /// </summary>
    public TimeSpan[] ReconnectIntervals { get; set; } =
    [
        TimeSpan.Zero,
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(10),
        TimeSpan.FromSeconds(30)
    ];
}
