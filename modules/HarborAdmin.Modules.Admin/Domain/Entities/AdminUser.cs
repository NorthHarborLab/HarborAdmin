using FreeSql.DataAnnotations;
using HarborAdmin.BuildingBlocks.Abstractions.Domain;

namespace HarborAdmin.Modules.Admin.Domain.Entities;

/// <summary>
/// Admin 登录用户。
/// </summary>
[DbKey("AdminDb")]
[Index("ux_admin_user_name", nameof(UserName), true)]
public sealed class AdminUser : EntityBase
{
    /// <summary>
    /// 登录名。
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// 显示名称。
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// 密码哈希。
    /// </summary>
    [Column(StringLength = -1)]
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// 部门 ID。
    /// </summary>
    public long? DeptId { get; set; }

    /// <summary>
    /// 首页路径。
    /// </summary>
    public string? HomePath { get; set; }

    /// <summary>
    /// 头像地址。
    /// </summary>
    public string? Avatar { get; set; }

    /// <summary>
    /// 备注。
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 是否启用。
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 创建时间。
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// 更新时间。
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; }
}
