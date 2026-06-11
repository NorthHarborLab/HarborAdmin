using HarborAdmin.BuildingBlocks.Abstractions.Application;
using HarborAdmin.BuildingBlocks.Mapping;
using HarborAdmin.Modules.AI.Application.Abstractions;
using HarborAdmin.Modules.AI.Application.Services.Shared;
using HarborAdmin.Modules.AI.Contracts.KnowledgeBase.Dto;
using HarborAdmin.Modules.AI.Contracts.KnowledgeBase.Request;
using HarborAdmin.Modules.AI.Domain.Entities;

namespace HarborAdmin.Modules.AI.Application.Services.KnowledgeBase;

/// <summary>
/// AI 知识库管理服务。
/// </summary>
public sealed class KnowledgeBaseService(IAiKnowledgeBaseRepository repository, IHarborMapper mapper)
    : HarborApplicationRepositoryService<AiKnowledgeBase, AiKnowledgeBaseDto, SaveAiKnowledgeBaseRequest, IAiKnowledgeBaseRepository>(repository)
{
    /// <inheritdoc />
    protected override AiKnowledgeBaseDto MapToDto(AiKnowledgeBase entity) => mapper.Map<AiKnowledgeBaseDto>(entity);

    /// <inheritdoc />
    protected override AiKnowledgeBase CreateEntity(SaveAiKnowledgeBaseRequest request) => new() { CreatedAt = UtcNow };

    /// <summary>
    /// 将保存请求应用到知识库。
    /// </summary>
    protected override Task ApplySaveAsync(AiKnowledgeBase entity, SaveAiKnowledgeBaseRequest request, CancellationToken cancellationToken)
    {
        entity.KnowledgeKey = AiNormalizationHelper.NormalizeKey(request.KnowledgeKey, nameof(request.KnowledgeKey));
        entity.Name = AiNormalizationHelper.NormalizeRequired(request.Name, nameof(request.Name));
        entity.ContentMarkdown = request.ContentMarkdown;
        entity.RetrievalType = AiNormalizationHelper.NormalizeRequired(request.RetrievalType, nameof(request.RetrievalType));
        entity.RetrievalOptionsJson = AiNormalizationHelper.NormalizeOptional(request.RetrievalOptionsJson);
        entity.AppendReferences = request.AppendReferences;
        entity.Enabled = request.Enabled;
        entity.UpdatedAt = UtcNow;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    protected override string GetNotFoundMessage(long id) => $"AI knowledge base '{id}' was not found.";
}
