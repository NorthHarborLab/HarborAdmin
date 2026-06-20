using HarborAdmin.BuildingBlocks.Abstractions.Repositories;
using HarborAdmin.Modules.AI.Domain.Entities;

namespace HarborAdmin.Modules.AI.Application.Abstractions;

/// <summary>
/// AI 供应商 CRUD 仓储。
/// </summary>
public interface IAiProviderRepository : IHarborCrudRepository<AiProvider>
{
    /// <summary>
    /// 按供应商 Key 获取供应商。
    /// </summary>
    Task<AiProvider?> GetProviderByKeyAsync(string providerKey, CancellationToken cancellationToken = default);
}
