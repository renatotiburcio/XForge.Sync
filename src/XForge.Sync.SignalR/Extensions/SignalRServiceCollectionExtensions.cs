using Microsoft.Extensions.DependencyInjection.Extensions;

namespace XForge.Sync.SignalR.Extensions;

/// <summary>
/// Extension methods for registering XForge.Sync.SignalR services in an <see cref="IServiceCollection"/>.
/// </summary>
public static class SignalRServiceCollectionExtensions
{
    /// <summary>
    /// Adds SignalR sync transport services to the specified <see cref="IServiceCollection"/>.
    /// Configures <see cref="SignalRSyncTransport"/> as the <see cref="ISyncTransport"/> implementation.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="configure">An optional action to configure <see cref="SignalRSyncTransportOptions"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddXForgeSyncSignalR(
        this IServiceCollection services,
        Action<SignalRSyncTransportOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (configure is not null)
        {
            services.Configure(configure);
        }

        services.TryAddSingleton<SignalRSyncTransport>();
        services.TryAddSingleton<ISyncTransport>(sp => sp.GetRequiredService<SignalRSyncTransport>());

        return services;
    }

    /// <summary>
    /// Adds SignalR sync transport services with options bound from configuration.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="configuration">The configuration to bind <see cref="SignalRSyncTransportOptions"/> from.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddXForgeSyncSignalR(
        this IServiceCollection services,
        Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<SignalRSyncTransportOptions>(
            configuration.GetSection(SignalRSyncTransportOptions.SectionName));

        services.TryAddSingleton<SignalRSyncTransport>();
        services.TryAddSingleton<ISyncTransport>(sp => sp.GetRequiredService<SignalRSyncTransport>());

        return services;
    }
}
