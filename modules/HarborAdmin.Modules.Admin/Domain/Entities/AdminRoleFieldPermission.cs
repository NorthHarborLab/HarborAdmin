using FreeSql.DataAnnotations;
using HarborAdmin.BuildingBlocks.Abstractions.Domain;

namespace HarborAdmin.Modules.Admin.Domain.Entities;

/// <summary>
/// Admin 角色字段权限。
/// </summary>
[Index("ux_admin_role_field_permission", $"{nameof(RoleId)},{nameof(AdminFeatureFieldId)}", true)]
[Index("idx_admin_role_field_permission_field_id", nameof(AdminFeatureFieldId), false)]
public sealed class AdminRoleFieldPermission : EntityBase
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
    /// 功能字段 ID。
    /// </summary>
    public long AdminFeatureFieldId { get; set; }

    /// <summary>
    /// 功能字段。
    /// </summary>
    [Navigate(nameof(AdminFeatureFieldId))]
    public AdminFeatureField AdminFeatureField { get; set; } = null!;

    /// <summary>
    /// 功能编码（冗余）。
    /// </summary>
    public string FeatureCode { get; set; } = string.Empty;

    /// <summary>
    /// 字段名（冗余）。
    /// </summary>
    public string FieldName { get; set; } = string.Empty;

    /// <summary>
    /// 是否可见。
    /// </summary>
    public bool Visible { get; set; } = true;

    /// <summary>
    /// 是否可编辑。
    /// </summary>
    public bool Editable { get; set; } = true;

    /// <summary>
    /// 是否可导出。
    /// </summary>
    public bool Exportable { get; set; } = true;

    /// <summary>
    /// 是否脱敏。
    /// </summary>
    public bool Masked { get; set; }
}
