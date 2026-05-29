using System.Text.Json;

namespace XForge.Sync.Conflict;

/// <summary>
/// Resolves conflicts using 3-way property-level merge (D7 decision).
/// Compares Base vs Local and Base vs Remote for each property.
/// Properties changed in only one side take that side's value.
/// Properties changed in both sides use the configured fallback strategy.
/// When no Base version is available, falls back to Last-Write-Wins.
/// </summary>
public sealed class PropertyMergeResolver(PropertyMergeOptions options) : IConflictResolver
{
    private readonly PropertyMergeOptions _options = options ?? throw new ArgumentNullException(nameof(options));

    /// <summary>
    /// Initializes a new instance with default options (UseLocal for double-changed properties).
    /// </summary>
    public PropertyMergeResolver()
        : this(new PropertyMergeOptions())
    {
    }

    /// <inheritdoc />
    public Task<ConflictResolution<T>> ResolveAsync<T>(SyncConflict<T> conflict, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(conflict);

        // No base version — fall back to Last-Write-Wins
        return conflict.Base is null
            ? ResolveWithoutBase(conflict)
            : Task.FromResult(MergeProperties(conflict));
    }

    private static Task<ConflictResolution<T>> ResolveWithoutBase<T>(SyncConflict<T> conflict)
    {
        return conflict.LocalModified >= conflict.RemoteModified
            ? Task.FromResult(ConflictResolution<T>.UseLocal(conflict.Local))
            : Task.FromResult(ConflictResolution<T>.UseRemote(conflict.Remote));
    }

    private ConflictResolution<T> MergeProperties<T>(SyncConflict<T> conflict)
    {
        JsonSerializerOptions jsonOptions = CreateJsonOptions();

        byte[] baseJson = JsonSerializer.SerializeToUtf8Bytes(conflict.Base!, jsonOptions);
        byte[] localJson = JsonSerializer.SerializeToUtf8Bytes(conflict.Local, jsonOptions);
        byte[] remoteJson = JsonSerializer.SerializeToUtf8Bytes(conflict.Remote, jsonOptions);

        using JsonDocument baseDoc = JsonDocument.Parse(baseJson);
        using JsonDocument localDoc = JsonDocument.Parse(localJson);
        using JsonDocument remoteDoc = JsonDocument.Parse(remoteJson);

        // Primitive type (string, int, etc.) — compare as whole values
        if (baseDoc.RootElement.ValueKind != JsonValueKind.Object)
        {
            return MergePrimitive<T>(baseDoc.RootElement, localDoc.RootElement, remoteDoc.RootElement, jsonOptions);
        }

        // Object type — merge per property
        return MergeObject<T>(baseDoc.RootElement, localDoc.RootElement, remoteDoc.RootElement, jsonOptions);
    }

    private ConflictResolution<T> MergePrimitive<T>(
        JsonElement baseVal,
        JsonElement localVal,
        JsonElement remoteVal,
        JsonSerializerOptions jsonOptions)
    {
        bool localChanged = !JsonElementEquals(baseVal, localVal);
        bool remoteChanged = !JsonElementEquals(baseVal, remoteVal);

        JsonElement result = !localChanged && !remoteChanged
            ? baseVal
            : localChanged && !remoteChanged
                ? localVal
                : localChanged
                    ? (_options.BothChangedStrategy == ConflictResolutionType.UseRemote ? remoteVal : localVal)
                    : remoteVal;

        byte[] mergedJson = JsonSerializer.SerializeToUtf8Bytes(result, jsonOptions);
        T mergedValue = JsonSerializer.Deserialize<T>(mergedJson, jsonOptions)!;

        return ConflictResolution<T>.Merge(mergedValue);
    }

    private ConflictResolution<T> MergeObject<T>(
        JsonElement baseRoot,
        JsonElement localRoot,
        JsonElement remoteRoot,
        JsonSerializerOptions jsonOptions)
    {
        Dictionary<string, JsonElement> baseProps = ToPropertyMap(baseRoot);
        Dictionary<string, JsonElement> localProps = ToPropertyMap(localRoot);
        Dictionary<string, JsonElement> remoteProps = ToPropertyMap(remoteRoot);

        Dictionary<string, JsonElement> merged = new(StringComparer.Ordinal);

        foreach (KeyValuePair<string, JsonElement> baseProp in baseProps)
        {
            string name = baseProp.Key;
            JsonElement baseVal = baseProp.Value;

            localProps.TryGetValue(name, out JsonElement localVal);
            remoteProps.TryGetValue(name, out JsonElement remoteVal);

            bool localChanged = !JsonElementEquals(baseVal, localVal);
            bool remoteChanged = !JsonElementEquals(baseVal, remoteVal);

            merged[name] = !localChanged && !remoteChanged
                ? baseVal
                : localChanged && !remoteChanged
                    ? localVal
                    : localChanged
                        ? ResolveDoubleChange(localVal, remoteVal)
                        : remoteVal;
        }

        // Add any new properties from local or remote that aren't in base
        foreach (KeyValuePair<string, JsonElement> prop in localProps)
        {
            if (!baseProps.ContainsKey(prop.Key) && !merged.ContainsKey(prop.Key))
            {
                merged[prop.Key] = prop.Value;
            }
        }

        foreach (KeyValuePair<string, JsonElement> prop in remoteProps)
        {
            if (!baseProps.ContainsKey(prop.Key) && !merged.ContainsKey(prop.Key))
            {
                merged[prop.Key] = prop.Value;
            }
        }

        byte[] mergedJsonBytes = JsonSerializer.SerializeToUtf8Bytes(merged, jsonOptions);
        T mergedValue = JsonSerializer.Deserialize<T>(mergedJsonBytes, jsonOptions)!;

        return ConflictResolution<T>.Merge(mergedValue);
    }

    private JsonElement ResolveDoubleChange(JsonElement localVal, JsonElement remoteVal)
    {
        return _options.BothChangedStrategy switch
        {
            ConflictResolutionType.UseRemote => remoteVal,
            _ => localVal
        };
    }

    private static JsonSerializerOptions CreateJsonOptions() => new()
    {
        PropertyNamingPolicy = null,
        PropertyNameCaseInsensitive = false,
        IncludeFields = false,
        WriteIndented = false
    };

    private static Dictionary<string, JsonElement> ToPropertyMap(JsonElement element)
    {
        Dictionary<string, JsonElement> map = new(StringComparer.Ordinal);
        foreach (JsonProperty prop in element.EnumerateObject())
        {
            map[prop.Name] = prop.Value.Clone();
        }

        return map;
    }

    private static bool JsonElementEquals(JsonElement a, JsonElement b)
    {
        return a.ValueKind == b.ValueKind && a.ValueKind switch
        {
            JsonValueKind.Null => true,
            JsonValueKind.True => true,
            JsonValueKind.False => true,
            _ => a.GetRawText() == b.GetRawText()
        };
    }
}
