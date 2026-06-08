using HarborAdmin.BuildingBlocks.Abstractions.Exception;
using HarborAdmin.BuildingBlocks.Mapping;
using HarborAdmin.Modules.AI.Application.Abstractions;
using HarborAdmin.Modules.AI.Application.Services.Shared;
using HarborAdmin.Modules.AI.Contracts.Provider.Dto;
using HarborAdmin.Modules.AI.Contracts.Provider.Request;
using HarborAdmin.Modules.AI.Contracts.Quota.Dto;
using HarborAdmin.Modules.AI.Contracts.Quota.Request;
using HarborAdmin.Modules.AI.Domain.Entities;

namespace HarborAdmin.Modules.AI.Application.Services.Quota;

/// <summary>
/// AI 限额管理服务。
/// </summary>
public sealed class QuotaService(IAiRepository repository, IHarborMapper mapper)
{
    /// <summary>
    /// 获取供应商限额。
    /// </summary>
    public async Task<AiProviderQuotaDto?> GetProviderQuotaAsync(long providerId, string? producerKey = null, CancellationToken cancellationToken = default)
    {
        var quota = await repository.GetProviderQuotaAsync(providerId, AiNormalizationHelper.NormalizeOptional(producerKey), cancellationToken);
        return quota is null ? null : mapper.Map<AiProviderQuotaDto>(quota);
    }

    /// <summary>
    /// 保存供应商限额。
    /// </summary>
    public async Task<AiProviderQuotaDto> SaveProviderQuotaAsync(long providerId, SaveAiProviderQuotaRequest request, CancellationToken cancellationToken = default)
    {
        _ = await repository.GetProviderAsync(providerId, cancellationToken) ?? throw new NotFoundDomainException($"AI provider '{providerId}' was not found.");
        var producerKey = AiNormalizationHelper.NormalizeOptional(request.ProducerKey);
        var quota = await repository.GetProviderQuotaAsync(providerId, producerKey, cancellationToken) ?? new AiProviderQuota { ProviderId = providerId };
        quota.ProducerKey = producerKey;
        quota.RequestsPerMinute = request.RequestsPerMinute;
        quota.RequestsPerDay = request.RequestsPerDay;
        quota.TokensPerDay = request.TokensPerDay;
        quota.TokensPerMonth = request.TokensPerMonth;
        quota.MonthlyBudget = request.MonthlyBudget;
        quota.Enabled = request.Enabled;
        return mapper.Map<AiProviderQuotaDto>(await repository.SaveProviderQuotaAsync(quota, cancellationToken));
    }

    /// <summary>
    /// 列出模型限额。
    /// </summary>
    public async Task<IReadOnlyList<AiModelQuotaDto>> ListModelQuotasAsync(CancellationToken cancellationToken = default) =>
        (await repository.ListModelQuotasAsync(cancellationToken))
        .Select(mapper.Map<AiModelQuotaDto>)
        .ToList();

    /// <summary>
    /// 保存模型限额。
    /// </summary>
    public async Task<AiModelQuotaDto> SaveModelQuotaAsync(long? id, SaveAiModelQuotaRequest request, CancellationToken cancellationToken = default)
    {
        var quota = id is > 0
            ? (await repository.ListModelQuotasAsync(cancellationToken)).FirstOrDefault(q => q.Id == id.Value)
              ?? throw new NotFoundDomainException($"AI model quota '{id}' was not found.")
            : new AiModelQuota();
        quota.ProviderKey = AiNormalizationHelper.NormalizeKey(request.ProviderKey, nameof(request.ProviderKey));
        quota.ModelName = AiNormalizationHelper.NormalizeOptional(request.ModelName);
        quota.EndpointKey = AiNormalizationHelper.NormalizeOptional(request.EndpointKey);
        quota.BusinessKey = AiNormalizationHelper.NormalizeOptional(request.BusinessKey);
        quota.ProducerKey = AiNormalizationHelper.NormalizeOptional(request.ProducerKey);
        quota.RequestsPerMinute = request.RequestsPerMinute;
        quota.TokensPerMinute = request.TokensPerMinute;
        quota.RequestsPerDay = request.RequestsPerDay;
        quota.TokensPerDay = request.TokensPerDay;
        quota.MonthlyBudget = request.MonthlyBudget;
        quota.Enabled = request.Enabled;
        return mapper.Map<AiModelQuotaDto>(await repository.SaveModelQuotaAsync(quota, cancellationToken));
    }

    /// <summary>
    /// 删除模型限额。
    /// </summary>
    public Task DeleteModelQuotaAsync(long id, CancellationToken cancellationToken = default) =>
        repository.DeleteModelQuotaAsync(id, cancellationToken);
}
