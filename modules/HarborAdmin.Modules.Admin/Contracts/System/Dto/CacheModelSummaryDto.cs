namespace HarborAdmin.Modules.Admin.Contracts.System.Dto;

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
