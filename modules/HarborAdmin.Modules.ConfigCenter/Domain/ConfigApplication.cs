using FreeSql.DataAnnotations;

namespace HarborAdmin.Modules.ConfigCenter.Domain;

/// <summary>
/// 配置中心注册的应用(业务服务标识)
/// </summary>
public class ConfigApplication
{
    /// <summary>
    /// 主键
    /// </summary>
    [Column(IsIdentity = true, IsPrimary = true)]
    public long Id { get; set; }

    /// <summary>
    /// 应用唯一标识,例如 <c>order-service</c>
    /// </summary>
    public string AppId { get; set; } = string.Empty;

    /// <summary>
    /// 显示名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 可选描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 创建时间（UTC）
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
