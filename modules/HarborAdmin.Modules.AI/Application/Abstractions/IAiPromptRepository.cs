using HarborAdmin.BuildingBlocks.Abstractions.Repositories;
using HarborAdmin.Modules.AI.Domain.Entities;

namespace HarborAdmin.Modules.AI.Application.Abstractions;

/// <summary>
/// AI Prompt 实体 CRUD 仓储。
/// </summary>
public interface IAiPromptRepository : IHarborCrudRepository<AiPrompt>
{
    /// <summary>
    /// 获取启用的 Prompt。
    /// </summary>
    Task<AiPrompt?> GetEnabledPromptAsync(string promptKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// 判断 Prompt Key 与版本是否已被其他记录使用。
    /// </summary>
    Task<bool> PromptVersionExistsAsync(string promptKey, int version, long? excludeId, CancellationToken cancellationToken = default);
}
