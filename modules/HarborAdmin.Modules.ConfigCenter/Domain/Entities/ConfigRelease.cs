namespace HarborAdmin.Modules.ConfigCenter.Domain.Entities;

/// <summary>
/// 一次配置发布记录，同一应用下版本号单调递增。
/// </summary>
[DbKey("ConfigCenterDb")]
[Index("ux_config_release_app_version", $"{nameof(AppId)},{nameof(Version)}", true)]
public sealed class ConfigRelease : EntityBase
{
    /// <summary>
    /// 所属应用标识。
    /// </summary>
    public string AppId { get; set; } = string.Empty;

    /// <summary>
    /// 发布版本号（从 1 开始递增）。
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// 发布操作人（可选）。
    /// </summary>
    public string? PublishedBy { get; set; }

    /// <summary>
    /// 发布时间（UTC）。
    /// </summary>
    public DateTimeOffset PublishedAt { get; set; }

    /// <summary>
    /// 所属应用。
    /// </summary>
    [Navigate(nameof(AppId))]
    public ConfigApplication? Application { get; set; }

    /// <summary>
    /// 发布快照项。
    /// </summary>
    [Navigate(nameof(ConfigReleaseItem.ReleaseId))]
    public List<ConfigReleaseItem> Items { get; set; } = [];
}
