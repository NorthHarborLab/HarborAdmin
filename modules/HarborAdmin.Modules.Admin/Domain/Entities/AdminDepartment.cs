using FreeSql.DataAnnotations;
using HarborAdmin.BuildingBlocks.Abstractions.Domain;

namespace HarborAdmin.Modules.Admin.Domain.Entities;

/// <summary>
/// Admin 组织部门。
/// </summary>
[DbKey("AdminDb")]
[Index("ux_admin_department_code", nameof(DeptCode), true)]
public sealed class AdminDepartment : EntityBase
{
    /// <summary>
    /// 部门编码。
    /// </summary>
    public string DeptCode { get; set; } = string.Empty;

    /// <summary>
    /// 上级部门 ID。
    /// </summary>
    public long? ParentId { get; set; }

    /// <summary>
    /// 部门名称。
    /// </summary>
    public string Name { get; set; } = string.Empty;

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
