using FreeSql.DataAnnotations;
using HarborAdmin.BuildingBlocks.Abstractions.Domain;
using HarborAdmin.Modules.Admin.Contracts.FeatureDesign;

namespace HarborAdmin.Modules.Admin.Domain.Entities;

/// <summary>
/// 功能资源表
/// </summary>
[Index("ux_admin_feature_code", nameof(FeatureCode), true)]
[Index("idx_admin_feature_parent", nameof(ParentId), false)]
public sealed class AdminFeature : AuditableEntity
{
    /// <summary>
    /// 父级功能分类 ID。
    /// </summary>
    public long? ParentId { get; set; }

    /// <summary>
    /// 父级功能分类。
    /// </summary>
    [Navigate(nameof(ParentId))]
    public AdminFeature? Parent { get; set; }

    /// <summary>
    /// 功能编码
    /// </summary>
    public string FeatureCode { get; set; } = string.Empty;

    /// <summary>
    /// 功能名称。
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// 功能类型：Static 或 Dynamic
    /// </summary>
    [Column(MapType = typeof(short))]
    public AdminFeatureType FeatureType { get; set; } = AdminFeatureType.Static;

    /// <summary>
    /// 节点类型：Category 或 Feature。
    /// </summary>
    [Column(MapType = typeof(short))]
    public AdminFeatureNodeType NodeType { get; set; } = AdminFeatureNodeType.Feature;

    /// <summary>
    /// 前端组件标识
    /// </summary>
    public string Component { get; set; } = string.Empty;

    /// <summary>
    /// 动态功能处理器标识
    /// </summary>
    public string? HandlerKey { get; set; }

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
    /// 展示顺序。
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// 子级功能节点。
    /// </summary>
    [Navigate(nameof(ParentId))]
    public List<AdminFeature> Children { get; set; } = [];

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
