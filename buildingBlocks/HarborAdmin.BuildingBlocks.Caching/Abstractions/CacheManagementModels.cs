namespace HarborAdmin.BuildingBlocks.Caching.Abstractions;

/// <summary>
/// 缓存 Provider 信息。
/// </summary>
public sealed record CacheProviderInfo(string Provider, string KeyPrefix);

/// <summary>
/// 缓存模型目录描述。
/// </summary>
public sealed record CacheModelDescriptor(
    string ModelTypeName,
    string DisplayName,
    string Module,
    int Order,
    string Description,
    string Prefix,
    string KeyTemplate,
    int? ExpirationSeconds,
    IReadOnlyList<string> TagTemplates,
    IReadOnlyList<string> SensitiveFields,
    bool SupportsBulkClear)
{
    /// <summary>
    /// 分组 prefix。
    /// </summary>
    public string GroupPrefix { get; init; } = string.Empty;

    /// <summary>
    /// 分组展示名称。
    /// </summary>
    public string GroupName { get; init; } = string.Empty;
}

/// <summary>
/// 缓存分组描述。
/// </summary>
public sealed record CacheGroupDescriptor(
    string GroupPrefix,
    string DisplayName,
    string Module,
    int Order,
    IReadOnlyList<CacheModelDescriptor> Models,
    int ActiveTagCount);

/// <summary>
/// 运行时 tag 信息。
/// </summary>
public sealed record CacheTagRuntimeInfo(string Tag, int KeyCount);

/// <summary>
/// 缓存原始条目。
/// </summary>
public sealed record CacheRawEntry(bool Found, string? Json, int SizeBytes);

/// <summary>
/// 缓存条目内容。
/// </summary>
public sealed record CacheEntryContent(
    string Key,
    bool Found,
    string? ModelTypeName,
    string? Json,
    int SizeBytes,
    bool Truncated);
