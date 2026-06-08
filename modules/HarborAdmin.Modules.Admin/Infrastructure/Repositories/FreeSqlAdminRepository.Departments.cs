using HarborAdmin.Modules.Admin.Domain.Entities;

namespace HarborAdmin.Modules.Admin.Infrastructure.Repositories;

/// <summary>
/// Admin 部门 FreeSql 实现。
/// </summary>
public sealed partial class FreeSqlAdminRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<AdminDepartment>> ListDepartmentsAsync(CancellationToken cancellationToken = default) =>
        await FreeSql.Select<AdminDepartment>()
            .OrderBy(dept => dept.Id)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<AdminDepartment?> GetDepartmentAsync(long id, CancellationToken cancellationToken = default) =>
        await FreeSql.Select<AdminDepartment>()
            .Where(dept => dept.Id == id)
            .ToOneAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<AdminDepartment>> GetDepartmentsByIdsAsync(IReadOnlyList<long> ids, CancellationToken cancellationToken = default) =>
        await FreeSql.Select<AdminDepartment>()
            .Where(dept => ids.Contains(dept.Id))
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public Task<bool> DepartmentHasUsersAsync(long id, CancellationToken cancellationToken = default) =>
        FreeSql.Select<AdminUser>().Where(user => user.DeptId == id).AnyAsync(cancellationToken);

    /// <inheritdoc />
    public Task<long> CountChildDepartmentsAsync(long parentId, CancellationToken cancellationToken = default) =>
        FreeSql.Select<AdminDepartment>().Where(dept => dept.ParentId == parentId).CountAsync(cancellationToken);

    /// <inheritdoc />
    public Task InsertDepartmentAsync(AdminDepartment department, CancellationToken cancellationToken = default) =>
        FreeSql.Insert(department).ExecuteAffrowsAsync(cancellationToken);

    /// <inheritdoc />
    public Task UpdateDepartmentAsync(AdminDepartment department, CancellationToken cancellationToken = default) =>
        FreeSql.Update<AdminDepartment>().SetSource(department).ExecuteAffrowsAsync(cancellationToken);

    /// <inheritdoc />
    public Task DeleteDepartmentAsync(long id, CancellationToken cancellationToken = default) =>
        FreeSql.Delete<AdminDepartment>().Where(dept => dept.Id == id).ExecuteAffrowsAsync(cancellationToken);
}
