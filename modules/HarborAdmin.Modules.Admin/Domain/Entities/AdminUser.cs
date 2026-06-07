using FreeSql.DataAnnotations;
using HarborAdmin.BuildingBlocks.Abstractions.Domain;

namespace HarborAdmin.Modules.Admin.Domain.Entities;

/// <summary>
/// Admin 登录用户。
/// </summary>
[DbKey("AdminDb")]
[Index("ux_admin_user_name", nameof(UserName), true)]
public sealed class AdminUser : AuditableEntity
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
    /// 所属部门。
    /// </summary>
    [Navigate(nameof(DeptId))]
    public AdminDepartment? Dept { get; set; }

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
    /// 是否为超级管理员（拥有全部权限）。
    /// </summary>
    public bool IsSuperAdmin { get; set; }

    /// <summary>
    /// 用户角色关系。
    /// </summary>
    [Navigate(nameof(AdminUserRole.UserId))]
    public List<AdminUserRole> UserRoles { get; set; } = [];

    /// <summary>
    /// 刷新令牌。
    /// </summary>
    [Navigate(nameof(AdminRefreshToken.UserId))]
    public List<AdminRefreshToken> RefreshTokens { get; set; } = [];
}
