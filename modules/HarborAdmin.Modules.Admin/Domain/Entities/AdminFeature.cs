using FreeSql.DataAnnotations;
using HarborAdmin.BuildingBlocks.Abstractions.Domain;

namespace HarborAdmin.Modules.Admin.Domain.Entities;

/// <summary>
/// 功能资源表
/// </summary>
[DbKey("AdminDb")]
[Index("ux_admin_feature_code", nameof(FeatureCode), true)]
public sealed class AdminFeature : AuditableEntity
{
    /// <summary>
    /// 功能编码
    /// </summary>
    public string FeatureCode { get; set; } = string.Empty;

    /// <summary>
    /// 功能名称国际化 Key
    /// </summary>
    public string NameKey { get; set; } = string.Empty;

    /// <summary>
    /// 功能名称兜底文本
    /// </summary>
    public string? NameFallback { get; set; }

    /// <summary>
    /// 功能类型：Static 或 Dynamic
    /// </summary>
    public string FeatureType { get; set; } = "Static";

    /// <summary>
    /// 前端组件标识
    /// </summary>
    public string Component { get; set; } = string.Empty;

    /// <summary>
    /// 动态功能处理器标识
    /// </summary>
    public string? HandlerKey { get; set; }

    /// <summary>
    /// 所属模块名称
    /// </summary>
    public string? ModuleName { get; set; }

    /// <summary>
    /// 默认路由路径
    /// </summary>
    public string? RoutePath { get; set; }

    /// <summary>
    /// Schema 版本号
    /// </summary>
    public int SchemaVersion { get; set; } = 1;

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 字段资源。
    /// </summary>
    [Navigate(nameof(AdminFeatureField.AdminFeatureId))]
    public List<AdminFeatureField> Fields { get; set; } = [];

    /// <summary>
    /// API 资源。
    /// </summary>
    [Navigate(nameof(AdminFeatureApi.AdminFeatureId))]
    public List<AdminFeatureApi> Apis { get; set; } = [];

    /// <summary>
    /// 动作权限点。
    /// </summary>
    [Navigate(nameof(AdminFeatureAction.AdminFeatureId))]
    public List<AdminFeatureAction> Actions { get; set; } = [];
}
