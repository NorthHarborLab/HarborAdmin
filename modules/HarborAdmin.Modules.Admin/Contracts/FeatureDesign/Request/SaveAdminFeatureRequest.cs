using System.ComponentModel.DataAnnotations;

namespace HarborAdmin.Modules.Admin.Contracts.FeatureDesign.Request;

/// <summary>
/// 保存 Feature 请求。
/// </summary>
public sealed class SaveAdminFeatureRequest
{
    /// <summary>
    /// 父级分类 ID。
    /// </summary>
    public long? ParentId { get; set; }

    /// <summary>
    /// 功能编码。
    /// </summary>
    [Required(ErrorMessage = "功能编码不能为空。")]
    [MaxLength(120)]
    public string FeatureCode { get; set; } = string.Empty;

    /// <summary>
    /// 功能名称。
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// 节点类型。
    /// </summary>
    public AdminFeatureNodeType NodeType { get; set; } = AdminFeatureNodeType.Feature;

    /// <summary>
    /// 功能类型。
    /// </summary>
    public AdminFeatureType FeatureType { get; set; } = AdminFeatureType.Static;

    /// <summary>
    /// 组件标识。
    /// </summary>
    [MaxLength(120)]
    public string Component { get; set; } = string.Empty;

    /// <summary>
    /// 动态组件处理器。
    /// </summary>
    public string? HandlerKey { get; set; }

    /// <summary>
    /// 路由路径。
    /// </summary>
    public string? RoutePath { get; set; }

    /// <summary>
    /// 是否启用。
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// 排序。
    /// </summary>
    public int SortOrder { get; set; }
}
