using FreeSql.DataAnnotations;
using HarborAdmin.BuildingBlocks.Abstractions.Domain;

namespace HarborAdmin.Modules.AI.Domain.Entities;

/// <summary>
/// AI 配置发布快照。
/// </summary>
[DbKey("AdminDb")]
[Index("ux_ai_config_release_version", "Version", true)]
public class AiConfigRelease : EntityBase
{
    /// <summary>
    /// 发布版本。
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// 快照 JSON。
    /// </summary>
    [Column(StringLength = -1)]
    public string SnapshotJson { get; set; } = string.Empty;

    /// <summary>
    /// 快照校验和。
    /// </summary>
    public string Checksum { get; set; } = string.Empty;

    /// <summary>
    /// 发布人。
    /// </summary>
    public string? PublishedBy { get; set; }

    /// <summary>
    /// 发布备注。
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 是否当前活动版本。
    /// </summary>
    public bool Active { get; set; }

    /// <summary>
    /// 发布时间。
    /// </summary>
    public DateTimeOffset PublishedAt { get; set; }
}
