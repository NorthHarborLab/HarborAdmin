using HarborAdmin.BuildingBlocks.Abstractions.Application;
using HarborAdmin.BuildingBlocks.Mapping;
using HarborAdmin.Modules.AI.Application.Abstractions;
using HarborAdmin.Modules.AI.Application.Services.Shared;
using HarborAdmin.Modules.AI.Contracts.Business.Dto;
using HarborAdmin.Modules.AI.Contracts.Business.Request;
using HarborAdmin.Modules.AI.Contracts.Chat.Dto;
using HarborAdmin.Modules.AI.Domain.Entities;

namespace HarborAdmin.Modules.AI.Application.Services.Business;

/// <summary>
/// AI 业务管理服务。
/// </summary>
public sealed class BusinessService(IAiBusinessRepository repository, IHarborMapper mapper)
    : HarborApplicationRepositoryService<AiBusiness, AiBusinessDto, SaveAiBusinessRequest, IAiBusinessRepository>(repository)
{
    /// <inheritdoc />
    protected override AiBusinessDto MapToDto(AiBusiness entity) => mapper.Map<AiBusinessDto>(entity);

    /// <inheritdoc />
    protected override AiBusiness CreateEntity(SaveAiBusinessRequest request) => new() { CreatedAt = UtcNow };

    /// <summary>
    /// 将保存请求应用到业务。
    /// </summary>
    protected override Task ApplySaveAsync(AiBusiness entity, SaveAiBusinessRequest request, CancellationToken cancellationToken)
    {
        var now = UtcNow;
        entity.BusinessKey = AiNormalizationHelper.NormalizeKey(request.BusinessKey, nameof(request.BusinessKey));
        entity.Name = AiNormalizationHelper.NormalizeRequired(request.Name, nameof(request.Name));
        entity.AllowedProducerKeys = AiNormalizationHelper.NormalizeCsv(request.AllowedProducerKeys);
        entity.SigningSecretRef = AiNormalizationHelper.NormalizeOptional(request.SigningSecretRef);
        entity.CallbackTopic = AiNormalizationHelper.NormalizeOptional(request.CallbackTopic);
        entity.PromptKey = AiNormalizationHelper.NormalizeOptional(request.PromptKey);
        entity.KnowledgeKeys = AiNormalizationHelper.NormalizeCsv(request.KnowledgeKeys);
        entity.EnableStreaming = request.EnableStreaming;
        entity.AllowKnowledgeTextAppend = request.AllowKnowledgeTextAppend;
        entity.AllowKnowledgeTextOverride = request.AllowKnowledgeTextOverride;
        entity.MaxContextTokens = Math.Max(0, request.MaxContextTokens);
        entity.ContextOverflowStrategy = string.IsNullOrWhiteSpace(request.ContextOverflowStrategy) ? "Reject" : request.ContextOverflowStrategy.Trim();
        entity.FailureStrategy = string.IsNullOrWhiteSpace(request.FailureStrategy) ? "ReturnError" : request.FailureStrategy.Trim();
        entity.AllowModelOverride = request.AllowModelOverride;
        entity.AllowPromptOverride = request.AllowPromptOverride;
        entity.AllowKnowledgeText = request.AllowKnowledgeText;
        entity.AllowProviderOptionsOverride = request.AllowProviderOptionsOverride;
        entity.AllowToolOptionsOverride = request.AllowToolOptionsOverride;
        entity.Enabled = request.Enabled;
        entity.OutputFormat = AiNormalizationHelper.NormalizeOptional(request.OutputFormat);
        entity.OutputJsonSchema = AiNormalizationHelper.NormalizeOptional(request.OutputJsonSchema);
        entity.OutputStrict = request.OutputStrict;
        entity.OutputValidateAndRetry = request.OutputValidateAndRetry;
        entity.OutputMaxRetryCount = Math.Max(0, request.OutputMaxRetryCount);
        entity.ToolOptionsJson = AiNormalizationHelper.NormalizeOptional(request.ToolOptionsJson);
        entity.MaxToolRounds = Math.Max(0, request.MaxToolRounds);
        entity.ProviderOptionsJson = AiNormalizationHelper.NormalizeOptional(request.ProviderOptionsJson);
        entity.UpdatedAt = now;
        entity.Routes = request.Routes
            .OrderBy(r => r.Priority)
            .Select(r => new AiBusinessProviderRoute
            {
                ProviderKey = AiNormalizationHelper.NormalizeKey(r.ProviderKey, nameof(r.ProviderKey)),
                ModelOverride = AiNormalizationHelper.NormalizeOptional(r.ModelOverride),
                Priority = r.Priority,
                Enabled = r.Enabled,
                ProviderOptionsJson = AiNormalizationHelper.NormalizeOptional(r.ProviderOptionsJson),
                OpenRouterOptionsJson = AiNormalizationHelper.NormalizeOptional(r.OpenRouterOptionsJson)
            })
            .ToList();

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    protected override string GetNotFoundMessage(long id) => $"AI business '{id}' was not found.";

    /// <summary>
    /// 列出可用于聊天流式调用的业务选项。
    /// </summary>
    public async Task<IReadOnlyList<AiChatBusinessOptionDto>> ListChatOptionsAsync(CancellationToken cancellationToken = default)
    {
        var items = await Repository.ListAsync(cancellationToken);
        return items
            .Where(item => item.Enabled && item.EnableStreaming)
            .OrderBy(item => item.BusinessKey, StringComparer.Ordinal)
            .Select(item => new AiChatBusinessOptionDto(item.BusinessKey, item.Name, item.AllowedProducerKeys))
            .ToList();
    }
}
