using FreeSql.DataAnnotations;

namespace HarborAdmin.Modules.ConfigCenter.Domain;

/// <summary>
/// 草稿配置项;发布前仅在此表维护,发布后快照写入 <see cref="ConfigReleaseItem"/>.
/// </summary>
public class ConfigItem
{
    /// <summary>
    /// 主键
    /// </summary>
    [Column(IsIdentity = true, IsPrimary = true)]
    public long Id { get; set; }

    /// <summary>
    /// 所属应用标识
    /// </summary>
    public string AppId { get; set; } = string.Empty;

    /// <summary>
    /// 环境名称,例如 <c>Development</c>,<c>Production</c>
    /// </summary>
    public string Environment { get; set; } = string.Empty;

    /// <summary>
    /// 配置分组,对应 <c>IConfiguration</c> 中的节名
    /// </summary>
    public string Group { get; set; } = string.Empty;

    /// <summary>
    /// 配置键名
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// 配置值
    /// </summary>
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
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// 扁平化配置键,格式为 <c>Group:Key</c>;当 <see cref="Group"/> 为空时仅为 <see cref="Key"/>。
    /// </summary>
    public string ConfigKey => string.IsNullOrEmpty(Group) ? Key : $"{Group}:{Key}";
}
