using FreeSql.DataAnnotations;
using HarborAdmin.BuildingBlocks.Abstractions.Domain;

namespace HarborAdmin.Modules.Admin.Domain.Entities;

/// <summary>
/// Admin 组织部门。
/// </summary>
[DbKey("AdminDb")]
[Index("ux_admin_department_code", nameof(DeptCode), true)]
public sealed class AdminDepartment : AuditableEntity
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
    /// 上级部门。
    /// </summary>
    [Navigate(nameof(ParentId))]
    public AdminDepartment? Parent { get; set; }

    /// <summary>
    /// 子部门。
    /// </summary>
    [Navigate(nameof(ParentId))]
    public List<AdminDepartment> Children { get; set; } = [];

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
    /// 部门用户。
    /// </summary>
    [Navigate(nameof(AdminUser.DeptId))]
    public List<AdminUser> Users { get; set; } = [];
}
