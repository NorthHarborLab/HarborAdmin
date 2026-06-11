namespace HarborAdmin.Modules.ConfigCenter.Domain.Entities;

/// <summary>
/// 草稿配置项；发布前仅在此表维护，发布后快照写入 <see cref="ConfigReleaseItem"/>。
/// </summary>
[Index("ux_config_item_app_group_key", $"{nameof(AppId)},{nameof(Group)},{nameof(Key)}", true)]
public sealed class ConfigItem : AuditableEntity
{
    /// <summary>
    /// 所属应用标识
    /// </summary>
    public string AppId { get; set; } = string.Empty;

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
    /// 值类型提示,默认 <c>string</c>
    /// </summary>
    public string ValueType { get; set; } = "string";

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 扁平化配置键。存在 <see cref="Group"/> 时生成 <c>Group:Key</c>，否则保留 <see cref="Key"/>。
    /// </summary>
    public string ConfigKey => string.IsNullOrWhiteSpace(Group) ? Key : $"{Group.Trim()}:{Key}";

    /// <summary>
    /// 所属应用。
    /// </summary>
    [Navigate(nameof(AppId))]
    public ConfigApplication? Application { get; set; }
}
