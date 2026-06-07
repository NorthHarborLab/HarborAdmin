namespace HarborAdmin.Modules.Admin.Contracts.FeatureDesign.Dto;

/// <summary>
/// 功能 API。
/// </summary>
public sealed record AdminFeatureApiDto(
    long Id,
    string FeatureCode,
    string ApiCode,
    string NameKey,
    string? NameFallback,
    string Path,
    string HttpMethod,
    bool EnabledLog,
    bool EnabledParams,
    bool EnabledResult,
    int SortOrder,
    bool Enabled,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
