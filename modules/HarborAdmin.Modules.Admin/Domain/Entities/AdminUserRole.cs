using FreeSql.DataAnnotations;
using HarborAdmin.BuildingBlocks.Abstractions.Domain;

namespace HarborAdmin.Modules.Admin.Domain.Entities;

/// <summary>
/// Admin 用户角色关系。
/// </summary>
[DbKey("AdminDb")]
[Index("ux_admin_user_role", $"{nameof(UserId)},{nameof(RoleId)}", true)]
public sealed class AdminUserRole : EntityBase
{
    /// <summary>
    /// 用户 ID。
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// 用户。
    /// </summary>
    [Navigate(nameof(UserId))]
    public AdminUser User { get; set; } = null!;

    /// <summary>
    /// 角色 ID。
    /// </summary>
    public long RoleId { get; set; }

    /// <summary>
    /// 角色。
    /// </summary>
    [Navigate(nameof(RoleId))]
    public AdminRole Role { get; set; } = null!;
}
