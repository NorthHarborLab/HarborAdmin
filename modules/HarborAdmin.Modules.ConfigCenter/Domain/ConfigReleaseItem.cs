using FreeSql.DataAnnotations;
using HarborAdmin.BuildingBlocks.Abstractions.Domain;

namespace HarborAdmin.Modules.ConfigCenter.Domain;

/// <summary>
/// 发布快照中的单条配置,发布时从 <see cref="ConfigItem"/> 草稿复制
/// </summary>
[DbKey("ConfigCenterDb")]
public class ConfigReleaseItem : EntityBase
{
    /// <summary>
    /// 所属发布记录 <see cref="ConfigRelease.Id"/>
    /// </summary>
    public long ReleaseId { get; set; }

    /// <summary>
    /// 配置显示分组;不参与最终 <c>IConfiguration</c> 扁平键生成。
    /// </summary>
    public string Group { get; set; } = string.Empty;

    /// <summary>
    /// 配置键名
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
    /// 扁平化配置键。分组仅用于管理端展示,真实键由 <see cref="Key"/> 完整表达。
    /// </summary>
    public string ConfigKey => Key;
}
