using HarborAdmin.BuildingBlocks.Data;
using HarborAdmin.Modules.Admin.Application.Abstractions;
using HarborAdmin.Modules.Admin.Domain.Entities;
using HarborAdmin.Modules.Admin.Infrastructure.Contexts;

namespace HarborAdmin.Modules.Admin.Infrastructure.Repositories;

/// <summary>
/// Admin 用户聚合 FreeSql 仓储。
/// </summary>
public sealed class AdminUserRepository(IAdminDbContext db)
    : FreeSqlModuleRepository<IAdminDbContext>(db), IAdminUserRepository
{
    /// <inheritdoc />
    public async Task<AdminUser?> GetUserAggregateAsync(long userId, CancellationToken cancellationToken = default) =>
        await FreeSql.Select<AdminUser>()
            .Where(user => user.Id == userId)
            .Include(user => user.Dept)
            .IncludeMany(user => user.UserRoles, then => then.Include(link => link.Role))
            .ToOneAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<AdminUser>> ListUsersWithRolesAsync(CancellationToken cancellationToken = default) =>
        await FreeSql.Select<AdminUser>()
            .OrderBy(user => user.Id)
            .IncludeMany(user => user.UserRoles, then => then.Include(link => link.Role))
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task SaveUserAsync(AdminUser user, bool isUpdate, CancellationToken cancellationToken = default)
    {
        var repository = GetRepository<AdminUser>(cascadeSave: true);
        if (isUpdate)
        {
            await repository.UpdateAsync(user, cancellationToken);
        }
        else
        {
            await repository.InsertAsync(user, cancellationToken);
        }
    }

    /// <inheritdoc />
    public void SaveUserChildren(AdminUser user, string propertyName) =>
        GetRepository<AdminUser>(cascadeSave: true).SaveMany(user, propertyName);

    /// <inheritdoc />
    public Task DeleteUserCascadeAsync(long userId, CancellationToken cancellationToken = default) =>
        GetRepository<AdminUser>(cascadeSave: true).DeleteCascadeByDatabaseAsync(user => user.Id == userId, cancellationToken);
}
