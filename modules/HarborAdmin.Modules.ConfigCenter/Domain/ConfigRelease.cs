using HarborAdmin.BuildingBlocks.Abstractions.Domain;

namespace HarborAdmin.Modules.ConfigCenter.Domain;

/// <summary>
/// 一次配置发布记录,同一应用+环境下版本号单调递增
/// </summary>
[DbKey("ConfigCenterDb")]
public class ConfigRelease : EntityBase
{
    /// <summary>
    /// 所属应用标识
    /// </summary>
    public string AppId { get; set; } = string.Empty;

    /// <summary>
    /// 环境名称
    /// </summary>
    public string Environment { get; set; } = string.Empty;

    /// <summary>
    /// 发布版本号(从 1 开始递增)
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// 发布操作人(可选)
    /// </summary>
    public string? PublishedBy { get; set; }

    /// <summary>
    /// 发布时间(UTC)
    /// </summary>
    public DateTimeOffset PublishedAt { get; set; }
}
