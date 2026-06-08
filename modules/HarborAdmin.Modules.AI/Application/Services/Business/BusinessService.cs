using HarborAdmin.BuildingBlocks.Abstractions.Exception;
using HarborAdmin.BuildingBlocks.Mapping;
using HarborAdmin.Modules.AI.Application.Abstractions;
using HarborAdmin.Modules.AI.Application.Services.Shared;
using HarborAdmin.Modules.AI.Contracts.Business.Dto;
using HarborAdmin.Modules.AI.Contracts.Business.Request;
using HarborAdmin.Modules.AI.Domain.Entities;

namespace HarborAdmin.Modules.AI.Application.Services.Business;

/// <summary>
/// AI 业务管理服务。
/// </summary>
public sealed class BusinessService(IAiRepository repository, AiServiceContext context, IHarborMapper mapper)
{
    /// <summary>
    /// 列出业务。
    /// </summary>
    public async Task<IReadOnlyList<AiBusinessDto>> ListBusinessesAsync(CancellationToken cancellationToken = default) =>
        (await repository.ListBusinessesAsync(cancellationToken))
        .Select(mapper.Map<AiBusinessDto>)
        .ToList();

    /// <summary>
    /// 保存业务。
    /// </summary>
    public async Task<AiBusinessDto> SaveBusinessAsync(long? id, SaveAiBusinessRequest request, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var business = id is > 0
            ? await repository.GetBusinessAsync(id.Value, cancellationToken) ?? throw new NotFoundDomainException($"AI business '{id}' was not found.")
            : new AiBusiness { CreatedAt = now };
        business.BusinessKey = AiNormalizationHelper.NormalizeKey(request.BusinessKey, nameof(request.BusinessKey));
        business.Name = AiNormalizationHelper.NormalizeRequired(request.Name, nameof(request.Name));
        business.AllowedProducerKeys = AiNormalizationHelper.NormalizeCsv(request.AllowedProducerKeys);
        business.SigningSecretRef = AiNormalizationHelper.NormalizeOptional(request.SigningSecretRef);
        business.CallbackTopic = AiNormalizationHelper.NormalizeOptional(request.CallbackTopic);
        business.PromptKey = AiNormalizationHelper.NormalizeOptional(request.PromptKey);
        business.KnowledgeKeys = AiNormalizationHelper.NormalizeCsv(request.KnowledgeKeys);
        business.EnableStreaming = request.EnableStreaming;
        business.AllowKnowledgeTextAppend = request.AllowKnowledgeTextAppend;
        business.AllowKnowledgeTextOverride = request.AllowKnowledgeTextOverride;
        business.MaxContextTokens = Math.Max(0, request.MaxContextTokens);
        business.ContextOverflowStrategy = string.IsNullOrWhiteSpace(request.ContextOverflowStrategy) ? "Reject" : request.ContextOverflowStrategy.Trim();
        business.FailureStrategy = string.IsNullOrWhiteSpace(request.FailureStrategy) ? "ReturnError" : request.FailureStrategy.Trim();
        business.AllowModelOverride = request.AllowModelOverride;
        business.AllowPromptOverride = request.AllowPromptOverride;
        business.AllowKnowledgeText = request.AllowKnowledgeText;
        business.AllowProviderOptionsOverride = request.AllowProviderOptionsOverride;
        business.AllowToolOptionsOverride = request.AllowToolOptionsOverride;
        business.Enabled = request.Enabled;
        business.OutputFormat = AiNormalizationHelper.NormalizeOptional(request.OutputFormat);
        business.OutputJsonSchema = AiNormalizationHelper.NormalizeOptional(request.OutputJsonSchema);
        business.OutputStrict = request.OutputStrict;
        business.OutputValidateAndRetry = request.OutputValidateAndRetry;
        business.OutputMaxRetryCount = Math.Max(0, request.OutputMaxRetryCount);
        business.ToolOptionsJson = AiNormalizationHelper.NormalizeOptional(request.ToolOptionsJson);
        business.MaxToolRounds = Math.Max(0, request.MaxToolRounds);
        business.ProviderOptionsJson = AiNormalizationHelper.NormalizeOptional(request.ProviderOptionsJson);
        business.UpdatedAt = now;
        var routes = request.Routes
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
        AiBusiness saved;
        using var uow = context.UnitOfWorkManager.Begin(context.EntityRegistry.GetDbKey<AiBusiness>());
        using (context.DbContext.Bind(uow.Orm))
        {
            saved = await repository.SaveBusinessAsync(business, routes, cancellationToken);
        }

        uow.Commit();
        return mapper.Map<AiBusinessDto>(saved);
    }

    /// <summary>
    /// 删除业务。
    /// </summary>
    public Task DeleteBusinessAsync(long id, CancellationToken cancellationToken = default) =>
        repository.DeleteBusinessAsync(id, cancellationToken);
}