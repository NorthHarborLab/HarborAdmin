using HarborAdmin.BuildingBlocks.Abstractions.Results;

namespace HarborAdmin.Modules.Admin.Contracts.Shared.ErrorCode;

/// <summary>
/// 角色错误码。
/// </summary>
public static class AdminRoleErrorCodes
{
    /// <summary>
    /// 角色不存在。
    /// </summary>
    public static readonly HarborErrorDefinition NotFound = new(
        "ADMIN.ROLE.NOT_FOUND", HarborErrorKind.NotFound, "角色不存在。", "ADMIN", ArgumentNames: ["id"]);

    /// <summary>
    /// 绑定菜单不存在。
    /// </summary>
    public static readonly HarborErrorDefinition MenuNotFound = new(
        "ADMIN.ROLE.MENU_NOT_FOUND", HarborErrorKind.NotFound, "角色绑定的菜单不存在。", "ADMIN", ArgumentNames: ["ids"]);

    /// <summary>
    /// 绑定权限不存在。
    /// </summary>
    public static readonly HarborErrorDefinition PermissionNotFound = new(
        "ADMIN.ROLE.PERMISSION_NOT_FOUND", HarborErrorKind.NotFound, "角色绑定的权限不存在。", "ADMIN", ArgumentNames: ["permissionCode"]);

    /// <summary>
    /// 角色编码已存在。
    /// </summary>
    public static readonly HarborErrorDefinition DuplicateCode = new(
        "ADMIN.ROLE.DUPLICATE_CODE", HarborErrorKind.Conflict, "角色编码已存在。", "ADMIN", ArgumentNames: ["roleCode"]);
}
