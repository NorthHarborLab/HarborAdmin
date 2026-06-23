using HarborAdmin.BuildingBlocks.Data;
using HarborAdmin.BuildingBlocks.Data.Repositories;
using HarborAdmin.Modules.Admin.Application.Abstractions;
using HarborAdmin.Modules.Admin.Domain.Entities;
using HarborAdmin.Modules.Admin.Infrastructure.Contexts;

namespace HarborAdmin.Modules.Admin.Infrastructure.Repositories;

/// <summary>
/// JWT Profile 刷新令牌 FreeSql 仓储。
/// </summary>
public sealed class AdminJwtRefreshTokenRepository(IAdminDbContext db, UnitOfWorkManagerCloud unitOfWorkManager)
    : HarborRepository<IAdminDbContext>(db, unitOfWorkManager), IAdminJwtRefreshTokenRepository
{
    /// <inheritdoc />
    public async Task<AdminJwtRefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        await FreeSql.Select<AdminJwtRefreshToken>()
            .Where(item => item.TokenHash == tokenHash)
            .ToOneAsync(cancellationToken);

    /// <inheritdoc />
    public Task InsertAsync(AdminJwtRefreshToken token, CancellationToken cancellationToken = default) =>
        FreeSql.Insert(token).ExecuteAffrowsAsync(cancellationToken);

    /// <inheritdoc />
    public Task UpdateAsync(AdminJwtRefreshToken token, CancellationToken cancellationToken = default) =>
        FreeSql.Update<AdminJwtRefreshToken>().SetSource(token).ExecuteAffrowsAsync(cancellationToken);
}
