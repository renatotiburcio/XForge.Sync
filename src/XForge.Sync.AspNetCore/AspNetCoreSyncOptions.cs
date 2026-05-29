namespace XForge.Sync.AspNetCore;

/// <summary>
/// Configuration options for XForge.Sync ASP.NET Core server endpoints.
/// </summary>
public sealed class AspNetCoreSyncOptions
{
    /// <summary>
    /// The configuration section name.
    /// </summary>
    public const string SectionName = "XForge:Sync:AspNetCore";

    /// <summary>
    /// Gets or sets the base path for sync endpoints. Default is "/api/sync".
    /// </summary>
    public string BasePath { get; set; } = "/api/sync";

    /// <summary>
    /// Gets or sets whether to enable the health check endpoint at <c>{BasePath}/health</c>. Default is true.
    /// </summary>
    public bool EnableHealthEndpoint { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable detailed error messages in sync responses.
    /// Should be disabled in production for security. Default is false.
    /// </summary>
    public bool EnableDetailedErrors { get; set; }
}
