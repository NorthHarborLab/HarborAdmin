namespace HarborAdmin.Modules.Admin.Contracts.FeatureDesign.Dto;

/// <summary>
/// 功能设计 Feature。
/// </summary>
public sealed record AdminFeatureDto(
    long Id,
    string FeatureCode,
    string NameKey,
    string? NameFallback,
    string FeatureType,
    string Component,
    string? HandlerKey,
    string? ModuleName,
    string? RoutePath,
    int SchemaVersion,
    bool Enabled,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
