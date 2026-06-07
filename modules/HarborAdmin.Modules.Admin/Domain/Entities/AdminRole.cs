using FreeSql.DataAnnotations;
using HarborAdmin.BuildingBlocks.Abstractions.Domain;

namespace HarborAdmin.Modules.Admin.Domain.Entities;

/// <summary>
/// Admin 角色。
/// </summary>
[DbKey("AdminDb")]
[Index("ux_admin_role_code", nameof(RoleCode), true)]
public sealed class AdminRole : EntityBase
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
    /// 创建时间。
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// 更新时间。
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; }
}
