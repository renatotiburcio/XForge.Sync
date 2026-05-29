using Microsoft.Extensions.Configuration;

namespace XForge.Sync.AspNetCore.Extensions;

/// <summary>
/// Extension methods for registering XForge.Sync.AspNetCore services in an <see cref="IServiceCollection"/>.
/// </summary>
public static class AspNetCoreExtensions
{
    /// <summary>
    /// Adds XForge.Sync server-side services to the specified <see cref="IServiceCollection"/>.
    /// Registers ASP.NET Core sync options with default values.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddXForgeSyncServer(this IServiceCollection services)
    {
        return AddXForgeSyncServer(services, configure: null);
    }

    /// <summary>
    /// Adds XForge.Sync server-side services to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="configure">An optional action to configure <see cref="AspNetCoreSyncOptions"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddXForgeSyncServer(
        this IServiceCollection services,
        Action<AspNetCoreSyncOptions>? configure)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (configure is not null)
        {
            services.Configure(configure);
        }
        else
        {
            services.AddOptions<AspNetCoreSyncOptions>();
        }

        return services;
    }

    /// <summary>
    /// Adds XForge.Sync server-side services with options bound from configuration.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="configuration">The configuration to bind <see cref="AspNetCoreSyncOptions"/> from.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddXForgeSyncServer(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<AspNetCoreSyncOptions>(
            configuration.GetSection(AspNetCoreSyncOptions.SectionName));

        return services;
    }
}
