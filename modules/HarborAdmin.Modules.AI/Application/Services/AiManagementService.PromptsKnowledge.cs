using HarborAdmin.Modules.AI.Contracts.Dtos;
using HarborAdmin.Modules.AI.Contracts.Requests;
using HarborAdmin.Modules.AI.Domain.Entities;
using HarborAdmin.BuildingBlocks.Abstractions.Exception;

namespace HarborAdmin.Modules.AI.Application.Services;

public sealed partial class AiManagementService
{
    /// <summary>
    /// 列出 Prompt。
    /// </summary>
    public async Task<IReadOnlyList<AiPromptDto>> ListPromptsAsync(CancellationToken cancellationToken = default) =>
        (await repository.ListPromptsAsync(cancellationToken))
        .Select(prompt => mapper.Map<AiPromptDto>(prompt))
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
        prompt.PromptKey = NormalizeKey(request.PromptKey, nameof(request.PromptKey));
        prompt.Name = NormalizeRequired(request.Name, nameof(request.Name));
        prompt.Version = request.Version <= 0 ? 1 : request.Version;
        prompt.SystemPromptMarkdown = request.SystemPromptMarkdown;
        prompt.UserPromptMarkdown = request.UserPromptMarkdown;
        prompt.VariablesJson = NormalizeOptional(request.VariablesJson);
        prompt.Enabled = request.Enabled;
        prompt.UpdatedAt = now;
        return mapper.Map<AiPromptDto>(await repository.SavePromptAsync(prompt, cancellationToken));
    }

    /// <summary>
    /// 删除 Prompt。
    /// </summary>
    public Task DeletePromptAsync(long id, CancellationToken cancellationToken = default) =>
        repository.DeletePromptAsync(id, cancellationToken);

    /// <summary>
    /// 列出知识库。
    /// </summary>
    public async Task<IReadOnlyList<AiKnowledgeBaseDto>> ListKnowledgeBasesAsync(CancellationToken cancellationToken = default) =>
        (await repository.ListKnowledgeBasesAsync(cancellationToken))
        .Select(knowledgeBase => mapper.Map<AiKnowledgeBaseDto>(knowledgeBase))
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
        knowledgeBase.KnowledgeKey = NormalizeKey(request.KnowledgeKey, nameof(request.KnowledgeKey));
        knowledgeBase.Name = NormalizeRequired(request.Name, nameof(request.Name));
        knowledgeBase.ContentMarkdown = request.ContentMarkdown;
        knowledgeBase.RetrievalType = NormalizeRequired(request.RetrievalType, nameof(request.RetrievalType));
        knowledgeBase.RetrievalOptionsJson = NormalizeOptional(request.RetrievalOptionsJson);
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
