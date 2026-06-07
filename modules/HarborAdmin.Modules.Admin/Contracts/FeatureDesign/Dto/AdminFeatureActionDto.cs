namespace HarborAdmin.Modules.Admin.Contracts.FeatureDesign.Dto;

/// <summary>
/// 功能动作。
/// </summary>
public sealed record AdminFeatureActionDto(
    long Id,
    string FeatureCode,
    string ActionCode,
    string PermissionCode,
    string LabelKey,
    string? LabelFallback,
    int SortOrder,
    bool Enabled,
    IReadOnlyList<long> ApiIds,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
