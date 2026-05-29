using FreeSql.DataAnnotations;

namespace HarborAdmin.Modules.ConfigCenter.Domain;

/// <summary>
/// 发布快照中的单条配置,发布时从 <see cref="ConfigItem"/> 草稿复制
/// </summary>
public class ConfigReleaseItem
{
    /// <summary>
    /// 主键
    /// </summary>
    [Column(IsIdentity = true, IsPrimary = true)]
    public long Id { get; set; }

    /// <summary>
    /// 所属发布记录 <see cref="ConfigRelease.Id"/>
    /// </summary>
    public long ReleaseId { get; set; }

    /// <summary>
    /// 配置分组
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
    /// 值类型提示
    /// </summary>
    public string ValueType { get; set; } = "string";

    /// <summary>
    /// 扁平化配置键,格式为 <c>Group:Key</c>;当 <see cref="Group"/> 为空时仅为 <see cref="Key"/>。
    /// </summary>
    public string ConfigKey => string.IsNullOrEmpty(Group) ? Key : $"{Group}:{Key}";
}
