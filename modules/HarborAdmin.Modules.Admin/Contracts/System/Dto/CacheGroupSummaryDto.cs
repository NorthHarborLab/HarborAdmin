namespace HarborAdmin.Modules.Admin.Contracts.System.Dto;

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
