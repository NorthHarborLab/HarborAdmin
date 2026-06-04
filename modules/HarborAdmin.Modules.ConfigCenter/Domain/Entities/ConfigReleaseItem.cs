using FreeSql.DataAnnotations;
using HarborAdmin.BuildingBlocks.Abstractions.Domain;

namespace HarborAdmin.Modules.ConfigCenter.Domain.Entities;

/// <summary>
/// 发布快照中的单条配置,发布时从 <see cref="ConfigItem"/> 草稿复制
/// </summary>
[DbKey("ConfigCenterDb")]
[Index("ux_config_release_item_release_group_key", "ReleaseId,Group,Key", true)]
public class ConfigReleaseItem : EntityBase
{
    /// <summary>
    /// 所属发布记录主键。
    /// </summary>
    public long ReleaseId { get; set; }

    /// <summary>
    /// 配置根路径；非空时会参与最终 <c>IConfiguration</c> 扁平键生成。
    /// </summary>
    public string Group { get; set; } = string.Empty;

    /// <summary>
    /// 配置键名。
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// 配置值
    /// </summary>
    [Column(StringLength = -1)]
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// 值类型提示
    /// </summary>
    public string ValueType { get; set; } = "string";

    /// <summary>
    /// 扁平化配置键。存在 <see cref="Group"/> 时生成 <c>Group:Key</c>，否则保留 <see cref="Key"/>。
    /// </summary>
    public string ConfigKey => string.IsNullOrWhiteSpace(Group) ? Key : $"{Group.Trim()}:{Key}";
}
