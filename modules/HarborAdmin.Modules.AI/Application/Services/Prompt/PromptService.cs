using HarborAdmin.BuildingBlocks.Abstractions.Exception;
using HarborAdmin.BuildingBlocks.Mapping;
using HarborAdmin.Modules.AI.Application.Abstractions;
using HarborAdmin.Modules.AI.Application.Services.Shared;
using HarborAdmin.Modules.AI.Contracts.Prompt.Dto;
using HarborAdmin.Modules.AI.Contracts.Prompt.Request;
using HarborAdmin.Modules.AI.Domain.Entities;

namespace HarborAdmin.Modules.AI.Application.Services.Prompt;

/// <summary>
/// AI Prompt 管理服务。
/// </summary>
public sealed class PromptService(IAiRepository repository, IHarborMapper mapper)
{
    /// <summary>
    /// 列出 Prompt。
    /// </summary>
    public async Task<IReadOnlyList<AiPromptDto>> ListPromptsAsync(CancellationToken cancellationToken = default) =>
        (await repository.ListPromptsAsync(cancellationToken))
        .Select(mapper.Map<AiPromptDto>)
        .ToList();

    /// <summary>
    /// 保存 Prompt。
    /// </summary>
    public async Task<AiPromptDto> SavePromptAsync(long? id, SaveAiPromptRequest request, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var prompt = id is > 0
            ? await repository.GetPromptAsync(id.Value, cancellationToken) ?? throw new NotFoundDomainException($"AI prompt '{id}' was not found.")
            : new AiPrompt { CreatedAt = now };
        prompt.PromptKey = AiNormalizationHelper.NormalizeKey(request.PromptKey, nameof(request.PromptKey));
        prompt.Name = AiNormalizationHelper.NormalizeRequired(request.Name, nameof(request.Name));
        prompt.Version = request.Version <= 0 ? 1 : request.Version;
        prompt.SystemPromptMarkdown = request.SystemPromptMarkdown;
        prompt.UserPromptMarkdown = request.UserPromptMarkdown;
        prompt.VariablesJson = AiNormalizationHelper.NormalizeOptional(request.VariablesJson);
        prompt.Enabled = request.Enabled;
        prompt.UpdatedAt = now;
        return mapper.Map<AiPromptDto>(await repository.SavePromptAsync(prompt, cancellationToken));
    }

    /// <summary>
    /// 删除 Prompt。
    /// </summary>
    public Task DeletePromptAsync(long id, CancellationToken cancellationToken = default) =>
        repository.DeletePromptAsync(id, cancellationToken);
}
