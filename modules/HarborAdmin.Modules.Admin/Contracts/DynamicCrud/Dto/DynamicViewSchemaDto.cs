using System.Text.Json;

namespace HarborAdmin.Modules.Admin.Contracts.DynamicCrud.Dto;

/// <summary>
/// 动态视图 schema。
/// </summary>
public sealed record DynamicViewSchemaDto(
    string FeatureCode,
    string TitleKey,
    string? TitleFallback,
    string FeatureType,
    string Component,
    string? RoutePath,
    int SchemaVersion,
    IReadOnlyList<DynamicFieldSchemaDto> Columns,
    IReadOnlyList<DynamicFieldSchemaDto> SearchFields,
    IReadOnlyList<DynamicFieldSchemaDto> FormFields,
    IReadOnlyList<DynamicActionSchemaDto> Actions,
    DynamicEndpointSchemaDto? Endpoints);

/// <summary>
/// 动态字段 schema。
/// </summary>
public sealed record DynamicFieldSchemaDto(
    string Field,
    string LabelKey,
    string? LabelFallback,
    string? PlaceholderKey,
    string? PlaceholderFallback,
    string Component,
    string DataType,
    bool Required,
    bool Readonly,
    int Order,
    int? Width,
    IReadOnlyList<DynamicFieldOptionDto>? Options,
    JsonElement? Validation);

/// <summary>
/// 动态字段选项。
/// </summary>
public sealed record DynamicFieldOptionDto(
    string Label,
    JsonElement Value);

/// <summary>
/// 动态动作 schema。
/// </summary>
public sealed record DynamicActionSchemaDto(
    string Code,
    string LabelKey,
    string? LabelFallback,
    string? PermissionCode,
    int Order);

/// <summary>
/// 动态视图接口 schema。
/// </summary>
public sealed record DynamicEndpointSchemaDto(
    string? QueryPath,
    string? DetailPath,
    string? CreatePath,
    string? UpdatePath,
    string? DeletePath);
