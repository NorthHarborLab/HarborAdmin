namespace HarborAdmin.Modules.Admin.Contracts.System.Dto;

/// <summary>
/// 缓存管理概览。
/// </summary>
public sealed record CacheOverviewDto(
    CacheProviderInfoDto Provider,
    IReadOnlyList<CacheGroupSummaryDto> Groups);
