using HarborAdmin.Modules.AI.Domain.Entities;

namespace HarborAdmin.Modules.AI.Application.Abstractions;

public partial interface IAiRepository
{
    /// <summary>
    /// 列出 Prompt。
    /// </summary>
    Task<IReadOnlyList<AiPrompt>> ListPromptsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取启用的 Prompt。
    /// </summary>
    Task<AiPrompt?> GetEnabledPromptAsync(string promptKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// 按主键获取 Prompt。
    /// </summary>
    Task<AiPrompt?> GetPromptAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 保存 Prompt。
    /// </summary>
    Task<AiPrompt> SavePromptAsync(AiPrompt prompt, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除 Prompt。
    /// </summary>
    Task DeletePromptAsync(long id, CancellationToken cancellationToken = default);
}
