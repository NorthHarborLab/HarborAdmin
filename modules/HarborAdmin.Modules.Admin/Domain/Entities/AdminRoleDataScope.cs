using FreeSql.DataAnnotations;
using HarborAdmin.BuildingBlocks.Abstractions.Domain;

namespace HarborAdmin.Modules.Admin.Domain.Entities;

/// <summary>
/// Admin 角色数据范围。
/// </summary>
[DbKey("AdminDb")]
[Index("ix_admin_role_data_scope", $"{nameof(RoleId)},{nameof(ScopeType)},{nameof(ScopeValueType)},{nameof(ScopeValueId)}", false)]
public sealed class AdminRoleDataScope : EntityBase
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
    /// 范围类型。
    /// </summary>
    public string ScopeType { get; set; } = string.Empty;

    /// <summary>
    /// 自定义范围值类型。
    /// </summary>
    public string? ScopeValueType { get; set; }

    /// <summary>
    /// 自定义范围值 ID。
    /// </summary>
    public long? ScopeValueId { get; set; }
}
