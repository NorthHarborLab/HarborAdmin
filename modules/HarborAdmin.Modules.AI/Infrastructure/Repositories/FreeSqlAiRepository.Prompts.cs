using HarborAdmin.Modules.AI.Domain.Entities;

namespace HarborAdmin.Modules.AI.Infrastructure.Repositories;

/// <summary>
/// 基于 FreeSql 的 AI Prompt 仓储实现 partial。
/// </summary>
public sealed partial class FreeSqlAiRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<AiPrompt>> ListPromptsAsync(CancellationToken cancellationToken = default) =>
        await FreeSql.Select<AiPrompt>().OrderBy(p => p.PromptKey).OrderByDescending(p => p.Version).ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<AiPrompt?> GetEnabledPromptAsync(string promptKey, CancellationToken cancellationToken = default) =>
        await FreeSql.Select<AiPrompt>().Where(p => p.PromptKey == promptKey && p.Enabled).OrderByDescending(p => p.Version).FirstAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<AiPrompt?> GetPromptAsync(long id, CancellationToken cancellationToken = default) =>
        await FreeSql.Select<AiPrompt>().Where(p => p.Id == id).FirstAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<AiPrompt> SavePromptAsync(AiPrompt prompt, CancellationToken cancellationToken = default)
    {
        if (prompt.Id == 0)
        {
            var inserted = await FreeSql.Insert(prompt).ExecuteInsertedAsync(cancellationToken);
            prompt.Id = inserted.First().Id;
            return prompt;
        }

        await FreeSql.Update<AiPrompt>().SetSource(prompt).ExecuteAffrowsAsync(cancellationToken);
        return prompt;
    }

    /// <inheritdoc />
    public Task DeletePromptAsync(long id, CancellationToken cancellationToken = default) =>
        FreeSql.Delete<AiPrompt>().Where(p => p.Id == id).ExecuteAffrowsAsync(cancellationToken);
}
