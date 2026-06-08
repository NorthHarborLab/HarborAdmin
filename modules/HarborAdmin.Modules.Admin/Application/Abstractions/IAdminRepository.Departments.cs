using HarborAdmin.Modules.Admin.Domain.Entities;

namespace HarborAdmin.Modules.Admin.Application.Abstractions;

/// <summary>
/// Admin 部门仓储。
/// </summary>
public partial interface IAdminRepository
{
    /// <summary>
    /// 加载全部部门。
    /// </summary>
    Task<IReadOnlyList<AdminDepartment>> ListDepartmentsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 按 ID 加载部门。
    /// </summary>
    Task<AdminDepartment?> GetDepartmentAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 按 ID 批量加载部门。
    /// </summary>
    Task<IReadOnlyList<AdminDepartment>> GetDepartmentsByIdsAsync(IReadOnlyList<long> ids, CancellationToken cancellationToken = default);

    /// <summary>
    /// 判断部门下是否存在用户。
    /// </summary>
    Task<bool> DepartmentHasUsersAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 统计子部门数量。
    /// </summary>
    Task<long> CountChildDepartmentsAsync(long parentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 新增部门。
    /// </summary>
    Task InsertDepartmentAsync(AdminDepartment department, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新部门。
    /// </summary>
    Task UpdateDepartmentAsync(AdminDepartment department, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除部门。
    /// </summary>
    Task DeleteDepartmentAsync(long id, CancellationToken cancellationToken = default);
}
