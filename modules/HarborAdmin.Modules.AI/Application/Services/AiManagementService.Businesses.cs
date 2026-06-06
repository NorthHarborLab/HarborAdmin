using HarborAdmin.BuildingBlocks.Abstractions.Exception;
using HarborAdmin.Modules.AI.Contracts.Dtos;
using HarborAdmin.Modules.AI.Contracts.Requests;
using HarborAdmin.Modules.AI.Domain.Entities;

namespace HarborAdmin.Modules.AI.Application.Services;

public sealed partial class AiManagementService
{
    /// <summary>
    /// 列出业务。
    /// </summary>
    public async Task<IReadOnlyList<AiBusinessDto>> ListBusinessesAsync(CancellationToken cancellationToken = default) =>
        (await repository.ListBusinessesAsync(cancellationToken))
        .Select(business => mapper.Map<AiBusinessDto>(business))
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
        business.BusinessKey = NormalizeKey(request.BusinessKey, nameof(request.BusinessKey));
        business.Name = NormalizeRequired(request.Name, nameof(request.Name));
        business.AllowedProducerKeys = NormalizeCsv(request.AllowedProducerKeys);
        business.SigningSecretRef = NormalizeOptional(request.SigningSecretRef);
        business.CallbackTopic = NormalizeOptional(request.CallbackTopic);
        business.PromptKey = NormalizeOptional(request.PromptKey);
        business.KnowledgeKeys = NormalizeCsv(request.KnowledgeKeys);
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
        business.OutputFormat = NormalizeOptional(request.OutputFormat);
        business.OutputJsonSchema = NormalizeOptional(request.OutputJsonSchema);
        business.OutputStrict = request.OutputStrict;
        business.OutputValidateAndRetry = request.OutputValidateAndRetry;
        business.OutputMaxRetryCount = Math.Max(0, request.OutputMaxRetryCount);
        business.ToolOptionsJson = NormalizeOptional(request.ToolOptionsJson);
        business.MaxToolRounds = Math.Max(0, request.MaxToolRounds);
        business.ProviderOptionsJson = NormalizeOptional(request.ProviderOptionsJson);
        business.UpdatedAt = now;
        var routes = request.Routes
            .OrderBy(r => r.Priority)
            .Select(r => new AiBusinessProviderRoute
            {
                ProviderKey = NormalizeKey(r.ProviderKey, nameof(r.ProviderKey)),
                ModelOverride = NormalizeOptional(r.ModelOverride),
                Priority = r.Priority,
                Enabled = r.Enabled,
                ProviderOptionsJson = NormalizeOptional(r.ProviderOptionsJson),
                OpenRouterOptionsJson = NormalizeOptional(r.OpenRouterOptionsJson)
            })
            .ToList();
        AiBusiness saved;
        using var uow = unitOfWorkManager.Begin(entityRegistry.GetDbKey<AiBusiness>());
        using (dbContext.Bind(uow.Orm))
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
