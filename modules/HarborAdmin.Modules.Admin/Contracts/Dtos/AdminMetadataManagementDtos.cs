namespace HarborAdmin.Modules.Admin.Contracts.Dtos;

/// <summary>
/// Admin 动态资源维护 DTO。
/// </summary>
public sealed record AdminResourceDto(
    long Id,
    string ResourceCode,
    string NameKey,
    string? NameFallback,
    string? ModuleName,
    string? HandlerKey,
    bool Enabled,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>
/// Admin 动态视图维护 DTO。
/// </summary>
public sealed record AdminViewDto(
    long Id,
    string ViewCode,
    string ResourceCode,
    string TitleKey,
    string? TitleFallback,
    string ViewType,
    string? RoutePath,
    int SchemaVersion,
    bool Enabled,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>
/// Admin 动态字段维护 DTO。
/// </summary>
public sealed record AdminViewFieldDto(
    long Id,
    string ViewCode,
    string Field,
    string LabelKey,
    string? LabelFallback,
    string? PlaceholderKey,
    string? PlaceholderFallback,
    string Component,
    string DataType,
    bool ListVisible,
    bool SearchVisible,
    bool CreateVisible,
    bool UpdateVisible,
    bool Readonly,
    bool Required,
    int Order,
    int? Width,
    string? OptionsJson,
    string? ValidationJson,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>
/// Admin 动态动作维护 DTO。
/// </summary>
public sealed record AdminViewActionDto(
    long Id,
    string ViewCode,
    string ActionCode,
    string LabelKey,
    string? LabelFallback,
    string? PermissionCode,
    int Order,
    bool Enabled,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>
/// Admin 动态接口维护 DTO。
/// </summary>
public sealed record AdminViewEndpointManagementDto(
    long Id,
    string ViewCode,
    string? QueryPath,
    string? DetailPath,
    string? CreatePath,
    string? UpdatePath,
    string? DeletePath,
    bool Enabled,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
