using FreeSql.DataAnnotations;
using HarborAdmin.BuildingBlocks.Abstractions.Domain;

namespace HarborAdmin.Modules.Admin.Domain.Entities;

/// <summary>
/// Admin 会话访问包版本。
/// </summary>
[DbKey("AdminDb")]
[Index("ux_admin_session_version_key", nameof(VersionKey), true)]
public sealed class AdminSessionVersion : EntityBase
{
    /// <summary>
    /// 版本键。
    /// </summary>
    public string VersionKey { get; set; } = "global";

    /// <summary>
    /// 版本号。
    /// </summary>
    public long Version { get; set; } = 1;

    /// <summary>
    /// 更新时间。
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; }
}
