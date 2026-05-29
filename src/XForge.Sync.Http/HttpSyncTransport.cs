namespace XForge.Sync.Http;

/// <summary>
/// HTTP/REST implementation of <see cref="ISyncTransport"/> that sends sync requests
/// to a remote server using JSON over HTTP.
/// </summary>
public sealed class HttpSyncTransport : ISyncTransport
{
    private readonly HttpClient _httpClient;
    private readonly HttpSyncTransportOptions _options;
    private readonly ILogger<HttpSyncTransport> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Creates a new <see cref="HttpSyncTransport"/>.
    /// </summary>
    /// <param name="httpClient">The HTTP client for making requests.</param>
    /// <param name="options">The transport configuration options.</param>
    /// <param name="logger">The logger.</param>
    public HttpSyncTransport(
        HttpClient httpClient,
        IOptions<HttpSyncTransportOptions> options,
        ILogger<HttpSyncTransport> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = (options ?? throw new ArgumentNullException(nameof(options))).Value;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false
        };

        ConfigureHttpClient();
    }

    /// <inheritdoc />
    public async Task<SyncResponse> SendAsync(SyncRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        string endpoint = $"{_options.BaseUrl.TrimEnd('/')}{_options.SyncEndpoint}";
        _logger.LogDebug("Sending sync request to {Endpoint} with {ChangeCount} changes",
            endpoint, request.Changes.Count);

        int attempt = 0;
        Exception? lastException = null;

        while (attempt <= _options.MaxRetries)
        {
            try
            {
                using HttpResponseMessage response = await _httpClient.PostAsJsonAsync(
                    endpoint, request, _jsonOptions, ct);

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        SyncResponse? syncResponse = await response.Content.ReadFromJsonAsync<SyncResponse>(
                            _jsonOptions, ct);

                        if (syncResponse is null)
                        {
                            _logger.LogWarning("Received null sync response from {Endpoint}", endpoint);
                            return new SyncResponse { IsSuccess = false, ErrorMessage = "Empty response from server" };
                        }

                        _logger.LogDebug("Sync response: success={IsSuccess}, changes={ChangeCount}, conflicts={ConflictCount}",
                            syncResponse.IsSuccess, syncResponse.Changes.Count, syncResponse.Conflicts.Count);

                        return syncResponse;
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning(ex, "Failed to deserialize sync response from {Endpoint}", endpoint);
                        return new SyncResponse { IsSuccess = false, ErrorMessage = $"Invalid JSON response from server: {ex.Message}" };
                    }
                }

                string errorBody = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("Sync request failed with {StatusCode}: {ErrorBody}",
                    response.StatusCode, errorBody);

                // Don't retry client errors (4xx) — only server errors (5xx) and network issues
                if ((int)response.StatusCode < 500)
                {
                    return new SyncResponse
                    {
                        IsSuccess = false,
                        ErrorMessage = $"Server returned {response.StatusCode}: {errorBody}"
                    };
                }

                lastException = new HttpRequestException(
                    $"Server returned {response.StatusCode}: {errorBody}");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "HTTP error on sync request attempt {Attempt}", attempt + 1);
                lastException = ex;
            }
            catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "Sync request timed out on attempt {Attempt}", attempt + 1);
                lastException = ex;
            }

            attempt++;

            if (attempt <= _options.MaxRetries)
            {
                TimeSpan delay = CalculateBackoff(attempt);
                _logger.LogDebug("Retrying in {Delay}ms (attempt {Attempt}/{MaxRetries})",
                    delay.TotalMilliseconds, attempt, _options.MaxRetries);
                await Task.Delay(delay, ct);
            }
        }

        _logger.LogError(lastException, "Sync request failed after {Attempts} attempts", attempt);
        return new SyncResponse
        {
            IsSuccess = false,
            ErrorMessage = $"Sync failed after {attempt} attempts: {lastException?.Message}"
        };
    }

    /// <inheritdoc />
    public async Task<bool> IsAvailableAsync(CancellationToken ct = default)
    {
        try
        {
            string healthUrl = $"{_options.BaseUrl.TrimEnd('/')}{_options.HealthEndpoint}";
            using HttpResponseMessage response = await _httpClient.GetAsync(healthUrl, ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Health check failed — server unavailable");
            return false;
        }
    }

    private void ConfigureHttpClient()
    {
        _httpClient.Timeout = _options.Timeout;

        foreach (KeyValuePair<string, string> header in _options.DefaultHeaders)
        {
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
        }
    }

    private TimeSpan CalculateBackoff(int attempt)
    {
        // Exponential backoff: baseDelay * 2^(attempt-1), with jitter
        double exponential = Math.Pow(2, attempt - 1);
        TimeSpan delay = TimeSpan.FromTicks((long)(_options.RetryBaseDelay.Ticks * exponential));

        // Cap at 30 seconds
        if (delay > TimeSpan.FromSeconds(30))
        {
            delay = TimeSpan.FromSeconds(30);
        }

        return delay;
    }
}
