using HarborAdmin.BuildingBlocks.Application;
using HarborAdmin.BuildingBlocks.Abstractions.Exception;
using HarborAdmin.BuildingBlocks.Abstractions.ModelResults;
using HarborAdmin.BuildingBlocks.Abstractions.Repositories;
using HarborAdmin.BuildingBlocks.Abstractions.Results;
using HarborAdmin.BuildingBlocks.Mapping;
using HarborAdmin.Modules.AI.Application.Abstractions;
using HarborAdmin.Modules.AI.Application.Services.Shared;
using HarborAdmin.Modules.AI.Contracts.KnowledgeBase.Dto;
using HarborAdmin.Modules.AI.Contracts.KnowledgeBase.Request;
using HarborAdmin.Modules.AI.Contracts.Shared.ErrorCode;
using HarborAdmin.Modules.AI.Domain.Entities;

namespace HarborAdmin.Modules.AI.Application.Services.KnowledgeBase;

/// <summary>
/// AI 知识库管理服务。
/// </summary>
public sealed class KnowledgeBaseService(IAiKnowledgeBaseRepository repository, IHarborMapper mapper)
    : HarborCrudApplicationService<AiKnowledgeBase, AiKnowledgeBaseDto, PageRequest, SaveAiKnowledgeBaseRequest, IAiKnowledgeBaseRepository>(repository)
{
    /// <inheritdoc />
    protected override AiKnowledgeBaseDto MapToDto(AiKnowledgeBase entity) => mapper.Map<AiKnowledgeBaseDto>(entity);

    /// <inheritdoc />
    protected override AiKnowledgeBase CreateEntity(SaveAiKnowledgeBaseRequest request) => new() { CreatedAt = UtcNow };

    /// <summary>
    /// 将保存请求应用到知识库。
    /// </summary>
    protected override async Task<HarborResult> ApplySaveAsync(AiKnowledgeBase entity, SaveAiKnowledgeBaseRequest request, CancellationToken cancellationToken)
    {
        try
        {
            entity.KnowledgeKey = AiNormalizationHelper.NormalizeKey(request.KnowledgeKey, nameof(request.KnowledgeKey));
            entity.Name = AiNormalizationHelper.NormalizeRequired(request.Name, nameof(request.Name));
            entity.ContentMarkdown = request.ContentMarkdown;
            entity.RetrievalType = AiNormalizationHelper.NormalizeRequired(request.RetrievalType, nameof(request.RetrievalType));
            entity.RetrievalOptionsJson = AiNormalizationHelper.NormalizeOptional(request.RetrievalOptionsJson);
            entity.AppendReferences = request.AppendReferences;
            entity.Enabled = request.Enabled;
            entity.UpdatedAt = UtcNow;
            if (await Repository.KnowledgeKeyExistsAsync(
                    entity.KnowledgeKey,
                    entity.Id > 0 ? entity.Id : null,
                    cancellationToken))
            {
                return HarborResult.Failure(AiKnowledgeBaseErrorCodes.DuplicateKey.Create(
                    new Dictionary<string, object?> { ["knowledgeKey"] = entity.KnowledgeKey }));
            }

            return HarborResult.Success();
        }
        catch (ValidationDomainException exception)
        {
            return HarborResult.Failure(AiKnowledgeBaseErrorCodes.InvalidInput.Create(
                new Dictionary<string, object?> { ["reason"] = exception.Message }, exception.Errors, exception.ErrorMeta));
        }
    }

    /// <inheritdoc />
    protected override HarborErrorDefinition NotFoundError => AiKnowledgeBaseErrorCodes.NotFound;
}
