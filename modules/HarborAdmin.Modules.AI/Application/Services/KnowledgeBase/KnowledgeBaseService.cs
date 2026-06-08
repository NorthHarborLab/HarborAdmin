using HarborAdmin.BuildingBlocks.Abstractions.Exception;
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
public sealed class KnowledgeBaseService(IAiRepository repository, IHarborMapper mapper)
{
    /// <summary>
    /// 列出知识库。
    /// </summary>
    public async Task<IReadOnlyList<AiKnowledgeBaseDto>> ListKnowledgeBasesAsync(CancellationToken cancellationToken = default) =>
        (await repository.ListKnowledgeBasesAsync(cancellationToken))
        .Select(mapper.Map<AiKnowledgeBaseDto>)
        .ToList();

    /// <summary>
    /// 保存知识库。
    /// </summary>
    public async Task<AiKnowledgeBaseDto> SaveKnowledgeBaseAsync(long? id, SaveAiKnowledgeBaseRequest request, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var knowledgeBase = id is > 0
            ? await repository.GetKnowledgeBaseAsync(id.Value, cancellationToken) ?? throw new NotFoundDomainException($"AI knowledge base '{id}' was not found.")
            : new AiKnowledgeBase { CreatedAt = now };
        knowledgeBase.KnowledgeKey = AiNormalizationHelper.NormalizeKey(request.KnowledgeKey, nameof(request.KnowledgeKey));
        knowledgeBase.Name = AiNormalizationHelper.NormalizeRequired(request.Name, nameof(request.Name));
        knowledgeBase.ContentMarkdown = request.ContentMarkdown;
        knowledgeBase.RetrievalType = AiNormalizationHelper.NormalizeRequired(request.RetrievalType, nameof(request.RetrievalType));
        knowledgeBase.RetrievalOptionsJson = AiNormalizationHelper.NormalizeOptional(request.RetrievalOptionsJson);
        knowledgeBase.AppendReferences = request.AppendReferences;
        knowledgeBase.Enabled = request.Enabled;
        knowledgeBase.UpdatedAt = now;
        return mapper.Map<AiKnowledgeBaseDto>(await repository.SaveKnowledgeBaseAsync(knowledgeBase, cancellationToken));
    }

    /// <summary>
    /// 删除知识库。
    /// </summary>
    public Task DeleteKnowledgeBaseAsync(long id, CancellationToken cancellationToken = default) =>
        repository.DeleteKnowledgeBaseAsync(id, cancellationToken);
}
