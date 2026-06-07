using FreeSql.DataAnnotations;
using HarborAdmin.BuildingBlocks.Abstractions.Domain;

namespace HarborAdmin.Modules.Admin.Domain.Entities;

/// <summary>
/// Admin 角色。
/// </summary>
[DbKey("AdminDb")]
[Index("ux_admin_role_code", nameof(RoleCode), true)]
public sealed class AdminRole : AuditableEntity
{
    /// <summary>
    /// 角色编码。
    /// </summary>
    public string RoleCode { get; set; } = string.Empty;

    /// <summary>
    /// 角色名称。
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 数据范围类型。
    /// </summary>
    public string DataScopeType { get; set; } = "Self";

    /// <summary>
    /// 备注。
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 是否启用。
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 用户角色关系。
    /// </summary>
    [Navigate(nameof(AdminUserRole.RoleId))]
    public List<AdminUserRole> UserRoles { get; set; } = [];

    /// <summary>
    /// 角色菜单关系。
    /// </summary>
    [Navigate(nameof(AdminRoleMenu.RoleId))]
    public List<AdminRoleMenu> RoleMenus { get; set; } = [];

    /// <summary>
    /// 角色权限关系。
    /// </summary>
    [Navigate(nameof(AdminRolePermission.RoleId))]
    public List<AdminRolePermission> RolePermissions { get; set; } = [];

    /// <summary>
    /// 角色字段权限。
    /// </summary>
    [Navigate(nameof(AdminRoleFieldPermission.RoleId))]
    public List<AdminRoleFieldPermission> FieldPermissions { get; set; } = [];

    /// <summary>
    /// 角色数据范围。
    /// </summary>
    [Navigate(nameof(AdminRoleDataScope.RoleId))]
    public List<AdminRoleDataScope> DataScopes { get; set; } = [];
}
