using System.Text.Json;
using HarborAdmin.Modules.Admin.Contracts.FeatureDesign;

namespace HarborAdmin.Modules.Admin.Contracts.Access.Dto;

/// <summary>
/// 前端功能资源包。
/// </summary>
/// <param name="FeatureCode">功能编码。</param>
/// <param name="Name">功能名称。</param>
/// <param name="FeatureType">功能类型。</param>
/// <param name="Component">前端组件标识。</param>
/// <param name="RoutePath">路由路径。</param>
/// <param name="SchemaVersion">Schema 版本。</param>
/// <param name="Fields">字段资源列表。</param>
/// <param name="Actions">动作资源列表。</param>
/// <param name="Endpoints">接口资源列表。</param>
public sealed record FeatureResourceDto(
    string FeatureCode,
    string? Name,
    AdminFeatureType FeatureType,
    string? Component,
    string? RoutePath,
    int SchemaVersion,
    IReadOnlyList<FeatureResourceFieldDto> Fields,
    IReadOnlyList<FeatureResourceActionDto> Actions,
    IReadOnlyList<FeatureResourceEndpointDto> Endpoints);

/// <summary>
/// 前端功能字段资源。
/// </summary>
/// <param name="Field">字段编码。</param>
/// <param name="LabelKey">字段标题国际化 Key。</param>
/// <param name="LabelFallback">字段标题兜底文本。</param>
/// <param name="PlaceholderKey">输入提示国际化 Key。</param>
/// <param name="PlaceholderFallback">输入提示兜底文本。</param>
/// <param name="Component">前端组件类型。</param>
/// <param name="DataType">字段数据类型。</param>
/// <param name="Required">是否必填。</param>
/// <param name="Readonly">是否只读。</param>
/// <param name="Order">展示顺序。</param>
/// <param name="Width">列表宽度。</param>
/// <param name="ListVisible">是否在列表中展示。</param>
/// <param name="SearchVisible">是否在查询表单中展示。</param>
/// <param name="Sortable">是否允许排序。</param>
/// <param name="CreateVisible">是否在新增表单中展示。</param>
/// <param name="UpdateVisible">是否在更新表单中展示。</param>
/// <param name="Visible">当前用户是否可见。</param>
/// <param name="Editable">当前用户是否可编辑。</param>
/// <param name="Exportable">当前用户是否可导出。</param>
/// <param name="Masked">当前用户是否需要脱敏。</param>
/// <param name="DictCode">关联字典编码。</param>
/// <param name="Options">字段选项。</param>
/// <param name="Validation">字段校验规则。</param>
public sealed record FeatureResourceFieldDto(
    string Field,
    string LabelKey,
    string? LabelFallback,
    string? PlaceholderKey,
    string? PlaceholderFallback,
    AdminFeatureFieldComponent Component,
    AdminFeatureFieldDataType DataType,
    bool Required,
    bool Readonly,
    int Order,
    int? Width,
    bool ListVisible,
    bool SearchVisible,
    bool Sortable,
    bool CreateVisible,
    bool UpdateVisible,
    bool Visible,
    bool Editable,
    bool Exportable,
    bool Masked,
    string? DictCode,
    IReadOnlyList<FeatureResourceFieldOptionDto>? Options,
    JsonElement? Validation);

/// <summary>
/// 前端功能字段选项。
/// </summary>
/// <param name="Label">选项文本。</param>
/// <param name="Value">选项值。</param>
public sealed record FeatureResourceFieldOptionDto(
    string Label,
    JsonElement Value,
    string? Color,
    bool Disabled);

/// <summary>
/// 前端功能动作资源。
/// </summary>
/// <param name="Code">动作编码。</param>
/// <param name="LabelKey">动作标题国际化 Key。</param>
/// <param name="LabelFallback">动作标题兜底文本。</param>
/// <param name="PermissionCode">权限编码。</param>
/// <param name="Order">展示顺序。</param>
public sealed record FeatureResourceActionDto(
    string Code,
    string LabelKey,
    string? LabelFallback,
    string PermissionCode,
    int Order);

/// <summary>
/// 前端功能接口资源。
/// </summary>
/// <param name="Code">接口编码。</param>
/// <param name="Path">接口路径模板。</param>
/// <param name="HttpMethod">HTTP 方法。</param>
public sealed record FeatureResourceEndpointDto(
    string Code,
    string Path,
    string HttpMethod);
