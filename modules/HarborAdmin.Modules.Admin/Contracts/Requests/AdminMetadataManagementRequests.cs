namespace HarborAdmin.Modules.Admin.Contracts.Requests;

/// <summary>
/// 保存动态资源请求。
/// </summary>
public sealed record SaveAdminResourceRequest(
    string ResourceCode,
    string NameKey,
    string? NameFallback,
    string? ModuleName,
    string? HandlerKey,
    bool Enabled);

/// <summary>
/// 保存动态视图请求。
/// </summary>
public sealed record SaveAdminViewRequest(
    string ViewCode,
    string ResourceCode,
    string TitleKey,
    string? TitleFallback,
    string ViewType,
    string? RoutePath,
    bool Enabled);

/// <summary>
/// 保存动态字段请求。
/// </summary>
public sealed record SaveAdminViewFieldRequest(
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
    string? ValidationJson);

/// <summary>
/// 保存动态动作请求。
/// </summary>
public sealed record SaveAdminViewActionRequest(
    string ActionCode,
    string LabelKey,
    string? LabelFallback,
    string? PermissionCode,
    int Order,
    bool Enabled);

/// <summary>
/// 保存动态接口请求。
/// </summary>
public sealed record SaveAdminViewEndpointRequest(
    string? QueryPath,
    string? DetailPath,
    string? CreatePath,
    string? UpdatePath,
    string? DeletePath,
    bool Enabled);
