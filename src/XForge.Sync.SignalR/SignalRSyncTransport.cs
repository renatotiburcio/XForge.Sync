namespace XForge.Sync.SignalR;

/// <summary>
/// SignalR implementation of <see cref="ISyncTransport"/> that sends sync requests
/// to a remote server via a real-time SignalR hub connection.
/// </summary>
public sealed class SignalRSyncTransport : ISyncTransport, IAsyncDisposable
{
    private readonly SignalRSyncTransportOptions _options;
    private readonly ILogger<SignalRSyncTransport> _logger;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private HubConnection? _hubConnection;
    private bool _disposed;

    /// <summary>
    /// Gets the current connection state of the SignalR hub.
    /// </summary>
    public HubConnectionState State => _hubConnection?.State ?? HubConnectionState.Disconnected;

    /// <summary>
    /// Occurs when the hub connection is reconnected after a connection loss.
    /// </summary>
    public event Func<string?, Task>? Reconnected;

    /// <summary>
    /// Occurs when the hub connection is lost.
    /// </summary>
    public event Func<Exception?, Task>? Reconnecting;

    /// <summary>
    /// Creates a new <see cref="SignalRSyncTransport"/>.
    /// </summary>
    /// <param name="options">The transport configuration options.</param>
    /// <param name="logger">The logger.</param>
    public SignalRSyncTransport(
        IOptions<SignalRSyncTransportOptions> options,
        ILogger<SignalRSyncTransport> logger)
    {
        _options = (options ?? throw new ArgumentNullException(nameof(options))).Value;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<SyncResponse> SendAsync(SyncRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ObjectDisposedException.ThrowIf(_disposed, this);

        await EnsureConnectedAsync(ct);

        _logger.LogDebug("Sending sync request via SignalR with {ChangeCount} changes",
            request.Changes.Count);

        int attempt = 0;
        Exception? lastException = null;

        while (attempt <= _options.MaxRetries)
        {
            try
            {
                SyncResponse? response = await _hubConnection!.InvokeAsync<SyncResponse>(
                    _options.SyncMethodName, request, ct);

                if (response is null)
                {
                    _logger.LogWarning("Received null sync response from hub method {Method}",
                        _options.SyncMethodName);
                    return new SyncResponse { IsSuccess = false, ErrorMessage = "Empty response from server" };
                }

                _logger.LogDebug("Sync response: success={IsSuccess}, changes={ChangeCount}, conflicts={ConflictCount}",
                    response.IsSuccess, response.Changes.Count, response.Conflicts.Count);

                return response;
            }
            catch (HubException ex)
            {
                _logger.LogWarning(ex, "Hub exception on sync request attempt {Attempt}: {Message}",
                    attempt + 1, ex.Message);
                lastException = ex;

                // Hub exceptions are typically not transient — don't retry
                return new SyncResponse
                {
                    IsSuccess = false,
                    ErrorMessage = $"Hub error: {ex.Message}"
                };
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Connection lost during sync request attempt {Attempt}", attempt + 1);
                lastException = ex;

                // Connection lost — try to reconnect
                await EnsureConnectedAsync(ct);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                _logger.LogWarning("Sync request timed out on attempt {Attempt}", attempt + 1);
                lastException = new TimeoutException("Sync request timed out");
            }
            catch (OperationCanceledException)
            {
                // User-requested cancellation — propagate immediately
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unexpected error on sync request attempt {Attempt}", attempt + 1);
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
        ObjectDisposedException.ThrowIf(_disposed, this);

        try
        {
            await EnsureConnectedAsync(ct);

            if (_hubConnection?.State != HubConnectionState.Connected)
            {
                return false;
            }

            // Ping the server to verify the connection is alive
            await _hubConnection.InvokeAsync<bool>(_options.PingMethodName, ct);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "SignalR health check failed — server unavailable");
            return false;
        }
    }

    /// <summary>
    /// Ensures the hub connection is established. Creates and starts the connection if needed.
    /// </summary>
    private async Task EnsureConnectedAsync(CancellationToken ct)
    {
        if (_hubConnection?.State == HubConnectionState.Connected)
        {
            return;
        }

        await _connectionLock.WaitAsync(ct);
        try
        {
            // Double-check after acquiring lock
            if (_hubConnection?.State == HubConnectionState.Connected)
            {
                return;
            }

            await CreateAndStartConnectionAsync(ct);
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    /// <summary>
    /// Creates and starts the SignalR hub connection.
    /// </summary>
    private async Task CreateAndStartConnectionAsync(CancellationToken ct)
    {
        // Dispose existing connection if any
        if (_hubConnection is not null)
        {
            await DisposeHubConnectionAsync();
        }

        _logger.LogInformation("Creating SignalR connection to {HubUrl}", _options.HubUrl);

        HubConnectionBuilder builder = new();

        builder.WithUrl(_options.HubUrl, httpOptions =>
        {
            // Configure access token
            if (_options.AccessTokenProvider is not null)
            {
                httpOptions.AccessTokenProvider = () => _options.AccessTokenProvider(ct);
            }

            // Configure custom headers
            foreach (KeyValuePair<string, string> header in _options.DefaultHeaders)
            {
                httpOptions.Headers[header.Key] = header.Value;
            }

            // Configure transport type
            if (_options.TransportType != HttpTransportType.None)
            {
                httpOptions.Transports = _options.TransportType;
            }
        });

        builder.WithKeepAliveInterval(_options.KeepAliveInterval);
        builder.WithServerTimeout(_options.ServerTimeout);

        if (_options.AutomaticReconnect)
        {
            builder.WithAutomaticReconnect(_options.ReconnectIntervals);
        }

        builder.AddJsonProtocol(jsonOptions =>
        {
            jsonOptions.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            jsonOptions.PayloadSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        });

        _hubConnection = builder.Build();

        // Wire up connection lifecycle events
        _hubConnection.Reconnecting += OnReconnecting;
        _hubConnection.Reconnected += OnReconnected;
        _hubConnection.Closed += OnClosed;

        await _hubConnection.StartAsync(ct);

        _logger.LogInformation("SignalR connection established to {HubUrl} (State: {State})",
            _options.HubUrl, _hubConnection.State);
    }

    private Task OnReconnecting(Exception? exception)
    {
        _logger.LogWarning(exception, "SignalR connection lost — attempting to reconnect");
        return Reconnecting?.Invoke(exception) ?? Task.CompletedTask;
    }

    private Task OnReconnected(string? connectionId)
    {
        _logger.LogInformation("SignalR reconnected (ConnectionId: {ConnectionId})", connectionId);
        return Reconnected?.Invoke(connectionId) ?? Task.CompletedTask;
    }

    private Task OnClosed(Exception? exception)
    {
        if (exception is not null)
        {
            _logger.LogWarning(exception, "SignalR connection closed with error");
        }
        else
        {
            _logger.LogInformation("SignalR connection closed");
        }

        return Task.CompletedTask;
    }

    private TimeSpan CalculateBackoff(int attempt)
    {
        double exponential = Math.Pow(2, attempt - 1);
        TimeSpan delay = TimeSpan.FromTicks((long)(_options.RetryBaseDelay.Ticks * exponential));

        if (delay > TimeSpan.FromSeconds(30))
        {
            delay = TimeSpan.FromSeconds(30);
        }

        return delay;
    }

    private async Task DisposeHubConnectionAsync()
    {
        if (_hubConnection is null)
        {
            return;
        }

        _hubConnection.Reconnecting -= OnReconnecting;
        _hubConnection.Reconnected -= OnReconnected;
        _hubConnection.Closed -= OnClosed;

        try
        {
            await _hubConnection.DisposeAsync();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error disposing hub connection");
        }

        _hubConnection = null;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        await DisposeHubConnectionAsync();
        _connectionLock.Dispose();
    }
}
