using HarborAdmin.BuildingBlocks.Data;
using HarborAdmin.BuildingBlocks.Data.Repositories;
using HarborAdmin.Modules.Admin.Application.Abstractions;
using HarborAdmin.Modules.Admin.Domain.Entities;
using HarborAdmin.Modules.Admin.Infrastructure.Contexts;

namespace HarborAdmin.Modules.Admin.Infrastructure.Repositories;

/// <summary>
/// JWT Profile FreeSql 仓储。
/// </summary>
public sealed class AdminJwtProfileRepository(IAdminDbContext db, UnitOfWorkManagerCloud unitOfWorkManager)
    : HarborRepository<IAdminDbContext>(db, unitOfWorkManager), IAdminJwtProfileRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<AdminJwtProfile>> ListAsync(CancellationToken cancellationToken = default) =>
        await FreeSql.Select<AdminJwtProfile>()
            .OrderBy(item => item.ProfileKey)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<AdminJwtProfile?> GetByProfileKeyAsync(string profileKey, CancellationToken cancellationToken = default) =>
        await FreeSql.Select<AdminJwtProfile>()
            .Where(item => item.ProfileKey == profileKey)
            .ToOneAsync(cancellationToken);

    /// <inheritdoc />
    public async Task SaveAsync(AdminJwtProfile profile, bool isUpdate, CancellationToken cancellationToken = default)
    {
        if (isUpdate)
        {
            await UpdateAsync(profile, cancellationToken);
            return;
        }

        await FreeSql.Insert(profile).ExecuteAffrowsAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task UpdateAsync(AdminJwtProfile profile, CancellationToken cancellationToken = default) =>
        FreeSql.Update<AdminJwtProfile>().SetSource(profile).ExecuteAffrowsAsync(cancellationToken);
}
