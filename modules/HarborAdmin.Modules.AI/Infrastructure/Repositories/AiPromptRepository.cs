using FreeSql;
using HarborAdmin.BuildingBlocks.Data;
using HarborAdmin.BuildingBlocks.Data.Configs;
using HarborAdmin.BuildingBlocks.Data.Repositories;
using HarborAdmin.Modules.AI.Application.Abstractions;
using HarborAdmin.Modules.AI.Domain.Entities;
using HarborAdmin.Modules.AI.Infrastructure.Contexts;

namespace HarborAdmin.Modules.AI.Infrastructure.Repositories;

/// <summary>
/// AI Prompt 实体 CRUD 仓储。
/// </summary>
public sealed class AiPromptRepository(
    IAiDbContext db,
    DbEntityRegistry entityRegistry,
    UnitOfWorkManagerCloud unitOfWorkManager)
    : FreeSqlCrudRepository<AiPrompt, IAiDbContext>(db, entityRegistry, unitOfWorkManager), IAiPromptRepository
{
    /// <inheritdoc />
    public async Task<AiPrompt?> GetEnabledPromptAsync(string promptKey, CancellationToken cancellationToken = default) =>
        await FreeSql.Select<AiPrompt>()
            .Where(prompt => prompt.PromptKey == promptKey && prompt.Enabled)
            .OrderByDescending(prompt => prompt.Version)
            .FirstAsync(cancellationToken);
}
