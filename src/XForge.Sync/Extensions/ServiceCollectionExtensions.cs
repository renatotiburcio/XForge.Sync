using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using XForge.Sync.Conflict;
using XForge.Sync.Connectivity;
using XForge.Sync.Engine;
using XForge.Sync.Policy;
using XForge.Sync.Queue;
using XForge.Sync.Tracking;

namespace XForge.Sync.Extensions;

/// <summary>
/// Extension methods for registering XForge.Sync services in an <see cref="IServiceCollection"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds core XForge.Sync services to the specified <see cref="IServiceCollection"/>.
    /// Registers sync engine, change tracker, queue, conflict resolver, policy, and connectivity monitor.
    /// Default conflict resolver is <see cref="LastWriteWinsResolver"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddXForgeSync(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IChangeTracker, ChangeTracker>();
        services.TryAddSingleton<ISyncQueue, SyncQueue>();
        services.TryAddSingleton<IConflictResolver, LastWriteWinsResolver>();
        services.TryAddSingleton<ISyncPolicy, DefaultSyncPolicy>();
        services.TryAddSingleton<IConnectivityMonitor, ConnectivityMonitor>();
        services.TryAddScoped<ISyncEngine, SyncEngine>();

        return services;
    }

    /// <summary>
    /// Registers <see cref="PropertyMergeResolver"/> as the <see cref="IConflictResolver"/>
    /// using the specified options. Call after <see cref="AddXForgeSync"/> to override the default resolver.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to configure.</param>
    /// <param name="configure">An action to configure the <see cref="PropertyMergeOptions"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection UsePropertyMergeResolver(this IServiceCollection services, Action<PropertyMergeOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        PropertyMergeOptions options = new();
        configure?.Invoke(options);

        services.AddSingleton<IConflictResolver>(new PropertyMergeResolver(options));

        return services;
    }
}
