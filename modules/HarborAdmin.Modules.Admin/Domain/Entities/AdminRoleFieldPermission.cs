using FreeSql.DataAnnotations;
using HarborAdmin.BuildingBlocks.Abstractions.Domain;

namespace HarborAdmin.Modules.Admin.Domain.Entities;

/// <summary>
/// Admin 角色字段权限。
/// </summary>
[DbKey("AdminDb")]
[Index("ux_admin_role_field_permission", $"{nameof(RoleId)},{nameof(FeatureCode)},{nameof(FieldName)}", true)]
public sealed class AdminRoleFieldPermission : EntityBase
{
    /// <summary>
    /// 角色 ID。
    /// </summary>
    public long RoleId { get; set; }

    /// <summary>
    /// 功能编码。
    /// </summary>
    public string FeatureCode { get; set; } = string.Empty;

    /// <summary>
    /// 字段名。
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
