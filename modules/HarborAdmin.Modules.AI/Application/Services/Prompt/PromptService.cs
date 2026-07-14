using HarborAdmin.BuildingBlocks.Application;
using HarborAdmin.BuildingBlocks.Abstractions.Exception;
using HarborAdmin.BuildingBlocks.Abstractions.ModelResults;
using HarborAdmin.BuildingBlocks.Abstractions.Repositories;
using HarborAdmin.BuildingBlocks.Abstractions.Results;
using HarborAdmin.BuildingBlocks.Mapping;
using HarborAdmin.Modules.AI.Application.Abstractions;
using HarborAdmin.Modules.AI.Application.Services.Shared;
using HarborAdmin.Modules.AI.Contracts.Prompt.Dto;
using HarborAdmin.Modules.AI.Contracts.Prompt.Request;
using HarborAdmin.Modules.AI.Contracts.Shared.ErrorCode;
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
    protected override async Task<HarborResult> ApplySaveAsync(AiPrompt entity, SaveAiPromptRequest request, CancellationToken cancellationToken)
    {
        try
        {
            entity.PromptKey = AiNormalizationHelper.NormalizeKey(request.PromptKey, nameof(request.PromptKey));
            entity.Name = AiNormalizationHelper.NormalizeRequired(request.Name, nameof(request.Name));
            entity.Version = request.Version <= 0 ? 1 : request.Version;
            entity.SystemPromptMarkdown = request.SystemPromptMarkdown;
            entity.UserPromptMarkdown = request.UserPromptMarkdown;
            entity.VariablesJson = AiNormalizationHelper.NormalizeOptional(request.VariablesJson);
            entity.Enabled = request.Enabled;
            entity.UpdatedAt = UtcNow;
            if (await Repository.PromptVersionExistsAsync(
                    entity.PromptKey,
                    entity.Version,
                    entity.Id > 0 ? entity.Id : null,
                    cancellationToken))
            {
                return HarborResult.Failure(AiPromptErrorCodes.DuplicateVersion.Create(
                    new Dictionary<string, object?>
                    {
                        ["promptKey"] = entity.PromptKey,
                        ["version"] = entity.Version,
                    }));
            }

            return HarborResult.Success();
        }
        catch (ValidationDomainException exception)
        {
            return HarborResult.Failure(AiPromptErrorCodes.InvalidInput.Create(
                new Dictionary<string, object?> { ["reason"] = exception.Message }, exception.Errors, exception.ErrorMeta));
        }
    }

    /// <inheritdoc />
    protected override HarborErrorDefinition NotFoundError => AiPromptErrorCodes.NotFound;
}
