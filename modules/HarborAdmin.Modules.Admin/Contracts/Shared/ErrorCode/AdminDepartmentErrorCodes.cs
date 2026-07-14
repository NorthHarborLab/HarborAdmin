using HarborAdmin.BuildingBlocks.Abstractions.Results;

namespace HarborAdmin.Modules.Admin.Contracts.Shared.ErrorCode;

/// <summary>
/// 部门错误码。
/// </summary>
public static class AdminDepartmentErrorCodes
{
    /// <summary>
    /// 部门不存在。
    /// </summary>
    public static readonly HarborErrorDefinition NotFound = new(
        "ADMIN.DEPT.NOT_FOUND", HarborErrorKind.NotFound, "部门不存在。", "ADMIN", ArgumentNames: ["id"]);

    /// <summary>
    /// 上级部门不存在。
    /// </summary>
    public static readonly HarborErrorDefinition ParentNotFound = new(
        "ADMIN.DEPT.PARENT_NOT_FOUND", HarborErrorKind.NotFound, "上级部门不存在。", "ADMIN", ArgumentNames: ["parentId"]);

    /// <summary>
    /// 上级部门关系无效。
    /// </summary>
    public static readonly HarborErrorDefinition InvalidParent = new(
        "ADMIN.DEPT.INVALID_PARENT", HarborErrorKind.Validation, "上级部门关系无效。", "ADMIN");

    /// <summary>
    /// 部门存在下级部门。
    /// </summary>
    public static readonly HarborErrorDefinition HasChildren = new(
        "ADMIN.DEPT.HAS_CHILDREN", HarborErrorKind.Conflict, "请先删除下级部门。", "ADMIN", ArgumentNames: ["id"]);

    /// <summary>
    /// 部门存在用户。
    /// </summary>
    public static readonly HarborErrorDefinition HasUsers = new(
        "ADMIN.DEPT.HAS_USERS", HarborErrorKind.Conflict, "部门下存在用户，不能删除。", "ADMIN", ArgumentNames: ["id"]);

    /// <summary>
    /// 部门编码已存在。
    /// </summary>
    public static readonly HarborErrorDefinition DuplicateCode = new(
        "ADMIN.DEPT.DUPLICATE_CODE", HarborErrorKind.Conflict, "部门编码已存在。", "ADMIN", ArgumentNames: ["deptCode"]);
}
