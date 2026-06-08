using FreeSql.DataAnnotations;
using HarborAdmin.BuildingBlocks.Abstractions.Domain;

namespace HarborAdmin.Modules.ConfigCenter.Domain.Entities;

/// <summary>
/// 配置中心注册的应用(业务服务标识)
/// </summary>
[DbKey("ConfigCenterDb")]
[Index("ux_config_application_appid", "AppId", true)]
public class ConfigApplication : EntityBase
{
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
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// 草稿配置项。
    /// </summary>
    [Navigate(nameof(AppId))]
    public List<ConfigItem> Items { get; set; } = [];

    /// <summary>
    /// 发布记录。
    /// </summary>
    [Navigate(nameof(AppId))]
    public List<ConfigRelease> Releases { get; set; } = [];
}
