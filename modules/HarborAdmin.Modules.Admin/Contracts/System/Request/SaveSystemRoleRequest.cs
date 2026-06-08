using System.ComponentModel.DataAnnotations;
using HarborAdmin.Modules.Admin.Contracts.System.Dto;

namespace HarborAdmin.Modules.Admin.Contracts.System.Request;

/// <summary>
/// 保存角色请求。
/// </summary>
public sealed class SaveSystemRoleRequest
{
    /// <summary>
    /// 角色名称。
    /// </summary>
    [Required(ErrorMessage = "角色名称不能为空。")]
    [MaxLength(64)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 角色编码。
    /// </summary>
    [MaxLength(64)]
    public string? RoleCode { get; set; }

    /// <summary>
    /// 菜单 ID 列表。
    /// </summary>
    public IReadOnlyList<string>? MenuIds { get; set; }

    /// <summary>
    /// 权限编码列表。
    /// </summary>
    public IReadOnlyList<string>? PermissionCodes { get; set; }

    /// <summary>
    /// 字段策略列表。
    /// </summary>
    public IReadOnlyList<SystemRoleFieldPolicyDto>? FieldPolicies { get; set; }

    /// <summary>
    /// 权限选择值（兼容旧字段）。
    /// </summary>
    public IReadOnlyList<string>? Permissions { get; set; }

    /// <summary>
    /// 备注。
    /// </summary>
    [MaxLength(500)]
    public string? Remark { get; set; }

    /// <summary>
    /// 状态：1 启用，0 禁用。
    /// </summary>
    [Range(0, 1, ErrorMessage = "角色状态不合法。")]
    public int Status { get; set; } = 1;

    /// <summary>
    /// 数据范围类型。
    /// </summary>
    [MaxLength(32)]
    public string? DataScopeType { get; set; }
}
