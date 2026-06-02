using FreeSql.DataAnnotations;
using HarborAdmin.BuildingBlocks.Abstractions.Domain;

namespace HarborAdmin.Modules.ConfigCenter.Domain;

/// <summary>
/// 草稿配置项;发布前仅在此表维护,发布后快照写入 <see cref="ConfigReleaseItem"/>.
/// </summary>
[DbKey("ConfigCenterDb")]
public class ConfigItem : EntityBase
{
    /// <summary>
    /// 所属应用标识
    /// </summary>
    public string AppId { get; set; } = string.Empty;

    /// <summary>
    /// 环境名称,例如 <c>Development</c>,<c>Production</c>
    /// </summary>
    public string Environment { get; set; } = string.Empty;

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
    /// 值类型提示,默认 <c>string</c>
    /// </summary>
    public string ValueType { get; set; } = "string";

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 最后更新时间（UTC）
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>
    /// 扁平化配置键。分组仅用于管理端展示,真实键由 <see cref="Key"/> 完整表达。
    /// </summary>
    public string ConfigKey => Key;
}
