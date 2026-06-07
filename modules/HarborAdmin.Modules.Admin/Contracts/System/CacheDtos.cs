namespace HarborAdmin.Modules.Admin.Contracts.System;

/// <summary>
/// 缓存 Provider 信息。
/// </summary>
public sealed record CacheProviderInfoDto(string Provider, string KeyPrefix);

/// <summary>
/// 缓存模型摘要。
/// </summary>
public sealed record CacheModelSummaryDto(
    string ModelTypeName,
    string DisplayName,
    string Prefix,
    string KeyTemplate,
    int? ExpirationSeconds,
    IReadOnlyList<string> TagTemplates,
    bool SupportsBulkClear);

/// <summary>
/// 缓存分组摘要。
/// </summary>
public sealed record CacheGroupSummaryDto(
    string GroupPrefix,
    string DisplayName,
    string Module,
    int ModelCount,
    int ActiveTagCount,
    bool SupportsBulkClear,
    IReadOnlyList<CacheModelSummaryDto> Models);

/// <summary>
/// 缓存管理概览。
/// </summary>
public sealed record CacheOverviewDto(
    CacheProviderInfoDto Provider,
    IReadOnlyList<CacheGroupSummaryDto> Groups);

/// <summary>
/// 运行时 tag 信息。
/// </summary>
public sealed record CacheTagDto(string Tag, int KeyCount);

/// <summary>
/// 缓存条目内容。
/// </summary>
public sealed record CacheEntryValueDto(
    string Key,
    bool Found,
    string? ModelTypeName,
    string? Json,
    int SizeBytes,
    bool Truncated);

/// <summary>
/// 按 key 失效请求。
/// </summary>
public sealed record InvalidateCacheKeyRequest(string Key);
