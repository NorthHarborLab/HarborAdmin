using FreeSql;
using HarborAdmin.BuildingBlocks.Data;
using HarborAdmin.BuildingBlocks.Data.Configs;
using HarborAdmin.BuildingBlocks.Data.Repositories;
using HarborAdmin.Modules.AI.Application.Abstractions;
using HarborAdmin.Modules.AI.Domain.Entities;
using HarborAdmin.Modules.AI.Infrastructure.Contexts;

namespace HarborAdmin.Modules.AI.Infrastructure.Repositories;

/// <summary>
/// AI 模型限额实体 CRUD 仓储。
/// </summary>
public sealed class AiModelQuotaRepository(
    IAiDbContext db,
    DbEntityRegistry entityRegistry,
    UnitOfWorkManagerCloud unitOfWorkManager)
    : FreeSqlCrudRepository<AiModelQuota, IAiDbContext>(db, entityRegistry, unitOfWorkManager), IAiModelQuotaRepository
{
}
