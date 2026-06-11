using FreeSql;
using HarborAdmin.BuildingBlocks.Data;
using HarborAdmin.Modules.AI.Application.Abstractions;
using HarborAdmin.Modules.AI.Domain.Entities;
using HarborAdmin.Modules.AI.Infrastructure.Contexts;

namespace HarborAdmin.Modules.AI.Infrastructure.Repositories;

/// <summary>
/// AI 模型限额实体 CRUD 仓储。
/// </summary>
public sealed class AiModelQuotaRepository(IAiDbContext db, DbEntityRegistry entityRegistry)
    : FreeSqlEntityRepository<AiModelQuota, IAiDbContext>(db, entityRegistry), IAiModelQuotaRepository
{
    /// <inheritdoc />
    protected override ISelect<AiModelQuota> BuildListQuery(ISelect<AiModelQuota> query) =>
        query.OrderBy(quota => quota.ProviderKey).OrderBy(quota => quota.ModelName);
}
