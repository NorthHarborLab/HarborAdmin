using System.ComponentModel.DataAnnotations;

namespace HarborAdmin.Modules.Admin.Contracts.System.Request;

/// <summary>
/// 保存用户请求。
/// </summary>
public sealed class SaveSystemUserRequest
{
    /// <summary>
    /// 显示名称。
    /// </summary>
    [Required(ErrorMessage = "用户名称不能为空。")]
    [MaxLength(64)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 登录用户名。
    /// </summary>
    [MaxLength(64)]
    public string? UserName { get; set; }

    /// <summary>
    /// 登录密码。
    /// </summary>
    [MaxLength(128)]
    public string? Password { get; set; }

    /// <summary>
    /// 部门 ID。
    /// </summary>
    [MaxLength(32)]
    public string? DeptId { get; set; }

    /// <summary>
    /// 权限选择值（兼容旧字段）。
    /// </summary>
    public IReadOnlyList<string>? Permissions { get; set; }

    /// <summary>
    /// 角色 ID 列表。
    /// </summary>
    public IReadOnlyList<string>? RoleIds { get; set; }

    /// <summary>
    /// 备注。
    /// </summary>
    [MaxLength(500)]
    public string? Remark { get; set; }

    /// <summary>
    /// 状态：1 启用，0 禁用。
    /// </summary>
    [Range(0, 1, ErrorMessage = "用户状态不合法。")]
    public int Status { get; set; } = 1;

    /// <summary>
    /// 是否超级管理员。
    /// </summary>
    public bool IsSuperAdmin { get; set; }
}
