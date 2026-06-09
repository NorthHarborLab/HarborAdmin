namespace HarborAdmin.Modules.Admin.Application.Services.Access;

/// <summary>
/// 字段权限作用面。
/// </summary>
public enum AdminFieldSurface
{
    /// <summary>
    /// 列表。
    /// </summary>
    List,

    /// <summary>
    /// 详情。
    /// </summary>
    Detail,

    /// <summary>
    /// 新增。
    /// </summary>
    Create,

    /// <summary>
    /// 更新。
    /// </summary>
    Update,

    /// <summary>
    /// 导出。
    /// </summary>
    Export,
}

/// <summary>
/// Admin 字段权限集合。
/// </summary>
/// <param name="IsSuperAdmin">是否超级管理员。</param>
/// <param name="VisibleFields">可见字段集合。</param>
/// <param name="EditableFields">可编辑字段集合。</param>
/// <param name="ExportableFields">可导出字段集合。</param>
/// <param name="MaskedFields">需要脱敏的字段集合。</param>
public sealed record AdminFieldPermissionSet(
    bool IsSuperAdmin,
    IReadOnlySet<string> VisibleFields,
    IReadOnlySet<string> EditableFields,
    IReadOnlySet<string> ExportableFields,
    IReadOnlySet<string> MaskedFields)
{
    /// <summary>
    /// 全字段权限。
    /// </summary>
    public static AdminFieldPermissionSet Full { get; } = new(
        true,
        new HashSet<string>(StringComparer.OrdinalIgnoreCase),
        new HashSet<string>(StringComparer.OrdinalIgnoreCase),
        new HashSet<string>(StringComparer.OrdinalIgnoreCase),
        new HashSet<string>(StringComparer.OrdinalIgnoreCase));
}