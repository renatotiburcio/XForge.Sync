using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using XForge.Sync.Storage;

namespace XForge.Sync.IndexedDB.Extensions;

/// <summary>
/// Extension methods for registering XForge.Sync.IndexedDB services in an <see cref="IServiceCollection"/>.
/// </summary>
public static class IndexedDbServiceCollectionExtensions
{
    /// <summary>
    /// Adds IndexedDB sync storage services for Blazor WASM to the specified <see cref="IServiceCollection"/>.
    /// Configures <see cref="IndexedDbLocalStorage"/> as <see cref="ILocalStorage"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="configure">An optional action to configure <see cref="IndexedDbOptions"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddXForgeSyncIndexedDb(
        this IServiceCollection services,
        Action<IndexedDbOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (configure is not null)
        {
            services.Configure(configure);
        }
        else
        {
            services.AddOptions<IndexedDbOptions>();
        }

        services.TryAddSingleton<ILocalStorage, IndexedDbLocalStorage>();

        return services;
    }

    /// <summary>
    /// Adds IndexedDB sync storage services with options bound from configuration.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="configuration">The configuration to bind <see cref="IndexedDbOptions"/> from.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddXForgeSyncIndexedDb(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<IndexedDbOptions>(opt =>
        {
            IConfigurationSection section = configuration.GetSection(IndexedDbOptions.SectionName);
            section.Bind(opt);
        });

        services.TryAddSingleton<ILocalStorage, IndexedDbLocalStorage>();

        return services;
    }
}
