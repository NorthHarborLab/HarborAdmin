using HarborAdmin.BuildingBlocks.Abstractions.Repositories;
using HarborAdmin.Modules.AI.Domain.Entities;

namespace HarborAdmin.Modules.AI.Application.Abstractions;

/// <summary>
/// AI 模型限额实体 CRUD 仓储。
/// </summary>
public interface IAiModelQuotaRepository : IHarborCrudRepository<AiModelQuota>
{
    /// <summary>
    /// 判断模型限额作用域是否已被其他记录使用。
    /// </summary>
    Task<bool> ScopeExistsAsync(
        string providerKey,
        string? modelName,
        string? businessKey,
        string? producerKey,
        long? excludeId,
        CancellationToken cancellationToken = default);
}
