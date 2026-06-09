namespace HarborAdmin.Modules.Admin.Contracts.FeatureDesign.Dto;

/// <summary>
/// 功能 API 树分组。
/// </summary>
public sealed record AdminFeatureApiTreeDto(
    string FeatureCode,
    string FeatureName,
    IReadOnlyList<AdminFeatureApiTreeItemDto> Apis);

/// <summary>
/// 功能 API 树节点。
/// </summary>
public sealed record AdminFeatureApiTreeItemDto(
    long Id,
    string FeatureCode,
    string ApiCode,
    string Label,
    string Path,
    string HttpMethod);
