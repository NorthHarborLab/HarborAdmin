using HarborAdmin.BuildingBlocks.Abstractions.Repositories;
using HarborAdmin.Modules.AI.Domain.Entities;

namespace HarborAdmin.Modules.AI.Application.Abstractions;

/// <summary>
/// AI 业务 CRUD 仓储。
/// </summary>
public interface IAiBusinessRepository : IHarborCrudRepository<AiBusiness>
{
    /// <summary>
    /// 按业务 Key 获取业务及路由。
    /// </summary>
    Task<AiBusiness?> GetBusinessByKeyAsync(string businessKey, CancellationToken cancellationToken = default);
}
