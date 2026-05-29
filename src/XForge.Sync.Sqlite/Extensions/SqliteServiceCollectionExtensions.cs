using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using XForge.Sync.Storage;
using XForge.Sync.Tracking;

namespace XForge.Sync.Sqlite.Extensions;

/// <summary>
/// Extension methods for registering XForge.Sync.Sqlite services in an <see cref="IServiceCollection"/>.
/// </summary>
public static class SqliteServiceCollectionExtensions
{
    /// <summary>
    /// Adds SQLite sync storage and change tracking services to the specified <see cref="IServiceCollection"/>.
    /// Configures <see cref="SqliteLocalStorage"/> as <see cref="ILocalStorage"/>
    /// and <see cref="SqliteChangeTracker"/> as <see cref="IChangeTracker"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="configure">An optional action to configure <see cref="SqliteSyncOptions"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddXForgeSyncSqlite(
        this IServiceCollection services,
        Action<SqliteSyncOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (configure is not null)
        {
            services.Configure(configure);
        }

        services.TryAddSingleton<ILocalStorage, SqliteLocalStorage>();
        services.TryAddSingleton<IChangeTracker, SqliteChangeTracker>();

        return services;
    }

    /// <summary>
    /// Adds SQLite sync storage services with options bound from configuration.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="configuration">The configuration to bind <see cref="SqliteSyncOptions"/> from.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddXForgeSyncSqlite(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<SqliteSyncOptions>(opt =>
        {
            IConfigurationSection section = configuration.GetSection(SqliteSyncOptions.SectionName);
            section.Bind(opt);
        });

        services.TryAddSingleton<ILocalStorage, SqliteLocalStorage>();
        services.TryAddSingleton<IChangeTracker, SqliteChangeTracker>();

        return services;
    }

    /// <summary>
    /// Adds SQLite sync storage services with a shared connection for both storage and change tracking.
    /// Useful for desktop/mobile scenarios where a single database file is preferred.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="connectionString">The SQLite connection string.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddXForgeSyncSqliteShared(
        this IServiceCollection services,
        string connectionString)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrEmpty(connectionString);

        services.Configure<SqliteSyncOptions>(opts => opts.ConnectionString = connectionString);

        // Register a single shared connection
        services.TryAddSingleton(_ =>
        {
            SqliteConnection connection = new(connectionString);
            connection.Open();
            return connection;
        });

        // Storage and change tracker will use the shared connection via DI
        services.TryAddSingleton<ILocalStorage>(sp =>
        {
            SqliteConnection connection = sp.GetRequiredService<SqliteConnection>();
            var options = Microsoft.Extensions.Options.Options.Create(
                new SqliteSyncOptions { ConnectionString = connectionString });
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<SqliteLocalStorage>>();
            return new SqliteLocalStorage(connection, options, logger);
        });

        services.TryAddSingleton<IChangeTracker>(sp =>
        {
            SqliteConnection connection = sp.GetRequiredService<SqliteConnection>();
            var options = Microsoft.Extensions.Options.Options.Create(
                new SqliteSyncOptions { ConnectionString = connectionString });
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<SqliteChangeTracker>>();
            return new SqliteChangeTracker(connection, options, logger);
        });

        return services;
    }
}
