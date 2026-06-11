using HarborAdmin.BuildingBlocks.Data;
using HarborAdmin.Modules.Admin.Application.Abstractions;
using HarborAdmin.Modules.Admin.Domain.Entities;
using HarborAdmin.Modules.Admin.Infrastructure.Contexts;

namespace HarborAdmin.Modules.Admin.Infrastructure.Repositories;

/// <summary>
/// Admin 匿名认证 FreeSql 仓储。
/// </summary>
public sealed class AdminAuthRepository(IAdminDbContext db) : FreeSqlModuleRepository<IAdminDbContext>(db), IAdminAuthRepository
{
    /// <inheritdoc />
    public async Task<AdminUser?> GetUserByUserNameAsync(string userName, CancellationToken cancellationToken = default) =>
        await FreeSql.Select<AdminUser>()
            .Where(item => item.UserName == userName)
            .ToOneAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<AdminUser?> GetUserByIdAsync(long userId, CancellationToken cancellationToken = default) =>
        await FreeSql.Select<AdminUser>()
            .Where(user => user.Id == userId)
            .ToOneAsync(cancellationToken);

    /// <inheritdoc />
    public Task UpdateUserPasswordHashAsync(AdminUser user, CancellationToken cancellationToken = default) =>
        FreeSql.Update<AdminUser>().SetSource(user).ExecuteAffrowsAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<AdminRefreshToken?> GetRefreshTokenByHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        await FreeSql.Select<AdminRefreshToken>()
            .Where(item => item.TokenHash == tokenHash)
            .ToOneAsync(cancellationToken);

    /// <inheritdoc />
    public Task InsertRefreshTokenAsync(AdminRefreshToken token, CancellationToken cancellationToken = default) =>
        FreeSql.Insert(token).ExecuteAffrowsAsync(cancellationToken);

    /// <inheritdoc />
    public Task UpdateRefreshTokenAsync(AdminRefreshToken token, CancellationToken cancellationToken = default) =>
        FreeSql.Update<AdminRefreshToken>().SetSource(token).ExecuteAffrowsAsync(cancellationToken);
}
