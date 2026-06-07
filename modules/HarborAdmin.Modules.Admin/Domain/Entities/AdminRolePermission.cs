using FreeSql.DataAnnotations;
using HarborAdmin.BuildingBlocks.Abstractions.Domain;

namespace HarborAdmin.Modules.Admin.Domain.Entities;

/// <summary>
/// Admin 角色权限关系。
/// </summary>
[DbKey("AdminDb")]
[Index("ux_admin_role_permission", $"{nameof(RoleId)},{nameof(PermissionCode)}", true)]
public sealed class AdminRolePermission : EntityBase
{
    /// <summary>
    /// 角色 ID。
    /// </summary>
    public long RoleId { get; set; }

    /// <summary>
    /// 权限编码。
    /// </summary>
    public string PermissionCode { get; set; } = string.Empty;
}
