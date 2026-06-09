namespace HarborAdmin.Modules.Admin.Contracts.FeatureDesign.Dto;

/// <summary>
/// 功能设计 Feature。
/// </summary>
public sealed record AdminFeatureDto(
    long Id,
    long? ParentId,
    string FeatureCode,
    string? Name,
    AdminFeatureType FeatureType,
    AdminFeatureNodeType NodeType,
    string Component,
    string? HandlerKey,
    string? RoutePath,
    int SchemaVersion,
    bool Enabled,
    int SortOrder,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    bool IsVirtual = false)
{
    /// <summary>
    /// 子节点。
    /// </summary>
    public IReadOnlyList<AdminFeatureDto> Children { get; init; } = [];
}
