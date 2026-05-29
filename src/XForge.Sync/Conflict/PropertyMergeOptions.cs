namespace XForge.Sync.Conflict;

/// <summary>
/// Options for configuring the <see cref="PropertyMergeResolver"/>.
/// </summary>
public sealed class PropertyMergeOptions
{
    /// <summary>
    /// Gets or sets the strategy to use when both local and remote changed the same property.
    /// Default is <see cref="ConflictResolutionType.UseLocal"/> (local wins for double-changed properties).
    /// </summary>
    public ConflictResolutionType BothChangedStrategy { get; set; } = ConflictResolutionType.UseLocal;
}
