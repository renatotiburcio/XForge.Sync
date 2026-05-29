namespace XForge.Sync.AspNetCore.Endpoints;

/// <summary>
/// Provides ASP.NET Core minimal API endpoints for XForge.Sync server-side operations.
/// </summary>
public static class SyncEndpoint
{

    /// <summary>
    /// Maps XForge.Sync endpoints to the specified <see cref="IEndpointRouteBuilder"/>.
    /// </summary>
    /// <param name="app">The endpoint route builder.</param>
    /// <returns>The <see cref="IEndpointRouteBuilder"/> so that additional calls can be chained.</returns>
    public static IEndpointRouteBuilder MapXForgeSync(this IEndpointRouteBuilder app)
    {
        return MapXForgeSync(app, configure: null);
    }

    /// <summary>
    /// Maps XForge.Sync endpoints to the specified <see cref="IEndpointRouteBuilder"/>.
    /// </summary>
    /// <param name="app">The endpoint route builder.</param>
    /// <param name="configure">An optional action to configure <see cref="AspNetCoreSyncOptions"/>.</param>
    /// <returns>The <see cref="IEndpointRouteBuilder"/> so that additional calls can be chained.</returns>
    public static IEndpointRouteBuilder MapXForgeSync(
        this IEndpointRouteBuilder app,
        Action<AspNetCoreSyncOptions>? configure)
    {
        ArgumentNullException.ThrowIfNull(app);

        AspNetCoreSyncOptions options = new();
        configure?.Invoke(options);

        RouteGroupBuilder group = app.MapGroup(options.BasePath);

        group.MapPost("/", HandleSync);

        if (options.EnableHealthEndpoint)
        {
            group.MapGet("/health", HandleHealth);
        }

        return app;
    }

    /// <summary>
    /// Handles POST /api/sync — processes a sync request (push + pull).
    /// </summary>
    internal static async Task<IResult> HandleSync(
        SyncRequest request,
        IServerSyncHandler handler,
        ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        ILogger logger = loggerFactory.CreateLogger("XForge.Sync.AspNetCore.SyncEndpoint");
        ILogger<SyncRequest> syncLogger = loggerFactory.CreateLogger<SyncRequest>();

        syncLogger.LogDebug(
            "Received sync request from ClientId={ClientId} with {ChangeCount} changes, LastServerVersion={LastServerVersion}",
            request.ClientId, request.Changes.Count, request.LastServerVersion);

        try
        {
            SyncResponse response = await handler.HandleSyncAsync(request, ct);

            syncLogger.LogDebug(
                "Sync response: IsSuccess={IsSuccess}, Changes={ChangeCount}, Conflicts={ConflictCount}",
                response.IsSuccess, response.Changes.Count, response.Conflicts.Count);

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing sync request from ClientId={ClientId}", request.ClientId);

            return Results.Ok(new SyncResponse
            {
                IsSuccess = false,
                ErrorMessage = "An internal error occurred while processing the sync request."
            });
        }
    }

    /// <summary>
    /// Handles GET /api/sync/health — returns server health status.
    /// </summary>
    internal static IResult HandleHealth()
    {
        return Results.Ok(new
        {
            Status = "healthy",
            Timestamp = DateTime.UtcNow,
            Service = "XForge.Sync"
        });
    }
}
