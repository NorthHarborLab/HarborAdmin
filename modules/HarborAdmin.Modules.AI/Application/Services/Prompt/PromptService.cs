using HarborAdmin.BuildingBlocks.Application;
using HarborAdmin.BuildingBlocks.Abstractions.Exception;
using HarborAdmin.BuildingBlocks.Abstractions.ModelResults;
using HarborAdmin.BuildingBlocks.Abstractions.Repositories;
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
public sealed class PromptService(IAiPromptRepository repository, IHarborMapper mapper)
    : HarborCrudApplicationService<AiPrompt, AiPromptDto, PageRequest, SaveAiPromptRequest, IAiPromptRepository>(repository)
{
    /// <inheritdoc />
    protected override AiPromptDto MapToDto(AiPrompt entity) => mapper.Map<AiPromptDto>(entity);

    /// <inheritdoc />
    protected override AiPrompt CreateEntity(SaveAiPromptRequest request) => new() { CreatedAt = UtcNow };

    /// <summary>
    /// 将保存请求应用到 Prompt。
    /// </summary>
    protected override Task ApplySaveAsync(AiPrompt entity, SaveAiPromptRequest request, CancellationToken cancellationToken)
    {
        entity.PromptKey = AiNormalizationHelper.NormalizeKey(request.PromptKey, nameof(request.PromptKey));
        entity.Name = AiNormalizationHelper.NormalizeRequired(request.Name, nameof(request.Name));
        entity.Version = request.Version <= 0 ? 1 : request.Version;
        entity.SystemPromptMarkdown = request.SystemPromptMarkdown;
        entity.UserPromptMarkdown = request.UserPromptMarkdown;
        entity.VariablesJson = AiNormalizationHelper.NormalizeOptional(request.VariablesJson);
        entity.Enabled = request.Enabled;
        entity.UpdatedAt = UtcNow;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    protected override string GetNotFoundMessage(long id) => $"AI prompt '{id}' was not found.";
}
