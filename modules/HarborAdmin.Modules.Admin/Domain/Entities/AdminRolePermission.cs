using FreeSql.DataAnnotations;
using HarborAdmin.BuildingBlocks.Abstractions.Domain;

namespace HarborAdmin.Modules.Admin.Domain.Entities;

/// <summary>
/// Admin 角色权限关系。
/// </summary>
[Index("ux_admin_role_permission", $"{nameof(RoleId)},{nameof(AdminFeatureActionId)}", true)]
[Index("idx_admin_role_permission_action_id", nameof(AdminFeatureActionId), false)]
public sealed class AdminRolePermission : EntityBase
{
    /// <summary>
    /// 角色 ID。
    /// </summary>
    public long RoleId { get; set; }

    /// <summary>
    /// 角色。
    /// </summary>
    [Navigate(nameof(RoleId))]
    public AdminRole Role { get; set; } = null!;

    /// <summary>
    /// 功能动作 ID。
    /// </summary>
    public long AdminFeatureActionId { get; set; }

    /// <summary>
    /// 功能动作。
    /// </summary>
    [Navigate(nameof(AdminFeatureActionId))]
    public AdminFeatureAction AdminFeatureAction { get; set; } = null!;

    /// <summary>
    /// 权限编码（冗余）。
    /// </summary>
    public string PermissionCode { get; set; } = string.Empty;
}
