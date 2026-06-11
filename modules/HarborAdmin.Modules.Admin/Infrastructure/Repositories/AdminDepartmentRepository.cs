using FreeSql;
using HarborAdmin.BuildingBlocks.Data;
using HarborAdmin.Modules.Admin.Application.Abstractions;
using HarborAdmin.Modules.Admin.Domain.Entities;
using HarborAdmin.Modules.Admin.Infrastructure.Contexts;

namespace HarborAdmin.Modules.Admin.Infrastructure.Repositories;

/// <summary>
/// Admin 部门实体 CRUD 仓储。
/// </summary>
public sealed class AdminDepartmentRepository(IAdminDbContext db, DbEntityRegistry entityRegistry)
    : FreeSqlEntityRepository<AdminDepartment, IAdminDbContext>(db, entityRegistry), IAdminDepartmentRepository
{
    /// <inheritdoc />
    protected override ISelect<AdminDepartment> BuildListQuery(ISelect<AdminDepartment> query) =>
        query.OrderBy(department => department.Id);

    /// <inheritdoc />
    public Task<bool> HasUsersAsync(long id, CancellationToken cancellationToken = default) =>
        FreeSql.Select<AdminUser>().Where(user => user.DeptId == id).AnyAsync(cancellationToken);

    /// <inheritdoc />
    public Task<long> CountChildrenAsync(long parentId, CancellationToken cancellationToken = default) =>
        FreeSql.Select<AdminDepartment>().Where(department => department.ParentId == parentId).CountAsync(cancellationToken);
}
