using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace XForge.Sync.Http.Extensions;

/// <summary>
/// Extension methods for registering XForge.Sync.Http services in an <see cref="IServiceCollection"/>.
/// </summary>
public static class HttpServiceCollectionExtensions
{
    /// <summary>
    /// Adds HTTP sync transport services to the specified <see cref="IServiceCollection"/>.
    /// Configures <see cref="HttpSyncTransport"/> as the <see cref="ISyncTransport"/> implementation.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="configure">An optional action to configure <see cref="HttpSyncTransportOptions"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddXForgeSyncHttp(
        this IServiceCollection services,
        Action<HttpSyncTransportOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (configure is not null)
        {
            services.Configure(configure);
        }

        services.AddHttpClient<ISyncTransport, HttpSyncTransport>();
        services.TryAddSingleton<ISyncTransport, HttpSyncTransport>();

        return services;
    }

    /// <summary>
    /// Adds HTTP sync transport services with options bound from configuration.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="configuration">The configuration to bind <see cref="HttpSyncTransportOptions"/> from.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddXForgeSyncHttp(
        this IServiceCollection services,
        Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<HttpSyncTransportOptions>(
            configuration.GetSection(HttpSyncTransportOptions.SectionName));

        services.AddHttpClient<ISyncTransport, HttpSyncTransport>();
        services.TryAddSingleton<ISyncTransport, HttpSyncTransport>();

        return services;
    }
}
