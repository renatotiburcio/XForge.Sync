namespace XForge.Sync.Http;

/// <summary>
/// Configuration options for <see cref="HttpSyncTransport"/>.
/// </summary>
public sealed class HttpSyncTransportOptions
{
    /// <summary>
    /// The configuration section name.
    /// </summary>
    public const string SectionName = "XForge:Sync:Http";

    /// <summary>
    /// Gets or sets the base URL of the sync server (e.g., "https://api.example.com").
    /// </summary>
    public required string BaseUrl { get; set; }

    /// <summary>
    /// Gets or sets the sync endpoint path. Default is "/api/sync".
    /// </summary>
    public string SyncEndpoint { get; set; } = "/api/sync";

    /// <summary>
    /// Gets or sets the health check endpoint path. Default is "/api/sync/health".
    /// </summary>
    public string HealthEndpoint { get; set; } = "/api/sync/health";

    /// <summary>
    /// Gets or sets the HTTP request timeout. Default is 30 seconds.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the maximum number of retry attempts for transient failures. Default is 3.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Gets or sets the base delay for exponential backoff between retries. Default is 1 second.
    /// </summary>
    public TimeSpan RetryBaseDelay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Gets or sets the name of the <see cref="HttpClient"/> to use from <see cref="System.Net.Http.IHttpClientFactory"/>.
    /// When set, the transport will use a named client instead of the default client.
    /// </summary>
    public string? HttpClientName { get; set; }

    /// <summary>
    /// Gets or sets custom HTTP headers to include in every sync request.
    /// Useful for authentication tokens or API keys.
    /// </summary>
    public Dictionary<string, string> DefaultHeaders { get; set; } = [];
}
