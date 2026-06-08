using HarborAdmin.Modules.Admin.Domain.Entities;

namespace HarborAdmin.Modules.Admin.Infrastructure.Repositories;

/// <summary>
/// Admin 部门 FreeSql 实现。
/// </summary>
public sealed partial class FreeSqlAdminRepository
{
    /// <inheritdoc />
    public Task<IReadOnlyList<AdminDepartment>> ListDepartmentsAsync(CancellationToken cancellationToken = default) =>
        FreeSql.Select<AdminDepartment>()
            .OrderBy(dept => dept.Id)
            .ToListAsync(cancellationToken)
            .ContinueWith(task => (IReadOnlyList<AdminDepartment>)task.Result, cancellationToken);

    /// <inheritdoc />
    public Task<AdminDepartment?> GetDepartmentAsync(long id, CancellationToken cancellationToken = default) =>
        FreeSql.Select<AdminDepartment>()
            .Where(dept => dept.Id == id)
            .ToOneAsync(cancellationToken);

    /// <inheritdoc />
    public Task<bool> DepartmentHasUsersAsync(long id, CancellationToken cancellationToken = default) =>
        FreeSql.Select<AdminUser>().Where(user => user.DeptId == id).AnyAsync(cancellationToken);

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
