using FreeSql;
using HarborAdmin.BuildingBlocks.Data;
using HarborAdmin.BuildingBlocks.Data.Configs;
using HarborAdmin.BuildingBlocks.Data.Repositories;
using HarborAdmin.Modules.Admin.Application.Abstractions;
using HarborAdmin.Modules.Admin.Domain.Entities;
using HarborAdmin.Modules.Admin.Infrastructure.Contexts;

namespace HarborAdmin.Modules.Admin.Infrastructure.Repositories;

/// <summary>
/// Admin 部门实体 CRUD 仓储。
/// </summary>
public sealed class AdminDepartmentRepository(
    IAdminDbContext db,
    DbEntityRegistry entityRegistry,
    UnitOfWorkManagerCloud unitOfWorkManager)
    : FreeSqlCrudRepository<AdminDepartment, IAdminDbContext>(db, entityRegistry, unitOfWorkManager), IAdminDepartmentRepository
{
    /// <inheritdoc />
    public Task<bool> HasUsersAsync(long id, CancellationToken cancellationToken = default) =>
        FreeSql.Select<AdminUser>().Where(user => user.DeptId == id).AnyAsync(cancellationToken);

    /// <inheritdoc />
    public Task<long> CountChildrenAsync(long parentId, CancellationToken cancellationToken = default) =>
        FreeSql.Select<AdminDepartment>().Where(department => department.ParentId == parentId).CountAsync(cancellationToken);

    /// <inheritdoc />
    public Task<bool> DeptCodeExistsAsync(string deptCode, long? excludeId, CancellationToken cancellationToken = default)
    {
        var query = FreeSql.Select<AdminDepartment>().Where(entity => entity.DeptCode == deptCode);
        if (excludeId.HasValue)
        {
            query = query.Where(entity => entity.Id != excludeId.Value);
        }

        return query.AnyAsync(cancellationToken);
    }
}
