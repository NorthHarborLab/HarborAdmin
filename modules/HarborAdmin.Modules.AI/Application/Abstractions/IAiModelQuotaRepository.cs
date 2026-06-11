using HarborAdmin.BuildingBlocks.Abstractions.Repositories;
using HarborAdmin.Modules.AI.Domain.Entities;

namespace HarborAdmin.Modules.AI.Application.Abstractions;

/// <summary>
/// AI 模型限额实体 CRUD 仓储。
/// </summary>
public interface IAiModelQuotaRepository : IHarborCrudRepository<AiModelQuota>
{
}
