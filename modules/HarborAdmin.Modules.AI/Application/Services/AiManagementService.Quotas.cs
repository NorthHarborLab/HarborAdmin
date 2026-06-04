using HarborAdmin.Modules.AI.Contracts.Dtos;
using HarborAdmin.Modules.AI.Contracts.Requests;
using HarborAdmin.Modules.AI.Domain.Entities;

namespace HarborAdmin.Modules.AI.Application.Services;

public sealed partial class AiManagementService
{
    /// <summary>
    /// 获取供应商限额。
    /// </summary>
    public async Task<AiProviderQuotaDto?> GetProviderQuotaAsync(long providerId, string? producerKey = null, CancellationToken cancellationToken = default)
    {
        var quota = await repository.GetProviderQuotaAsync(providerId, NormalizeOptional(producerKey), cancellationToken);
        return quota is null ? null : mapper.Map<AiProviderQuotaDto>(quota);
    }

    /// <summary>
    /// 保存供应商限额。
    /// </summary>
    public async Task<AiProviderQuotaDto> SaveProviderQuotaAsync(long providerId, SaveAiProviderQuotaRequest request, CancellationToken cancellationToken = default)
    {
        _ = await repository.GetProviderAsync(providerId, cancellationToken) ?? throw new KeyNotFoundException($"AI provider '{providerId}' was not found.");
        var producerKey = NormalizeOptional(request.ProducerKey);
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
        .Select(quota => mapper.Map<AiModelQuotaDto>(quota))
        .ToList();

    /// <summary>
    /// 保存模型限额。
    /// </summary>
    public async Task<AiModelQuotaDto> SaveModelQuotaAsync(long? id, SaveAiModelQuotaRequest request, CancellationToken cancellationToken = default)
    {
        var quota = id is > 0
            ? (await repository.ListModelQuotasAsync(cancellationToken)).FirstOrDefault(q => q.Id == id.Value)
              ?? throw new KeyNotFoundException($"AI model quota '{id}' was not found.")
            : new AiModelQuota();
        quota.ProviderKey = NormalizeKey(request.ProviderKey, nameof(request.ProviderKey));
        quota.ModelName = NormalizeOptional(request.ModelName);
        quota.EndpointKey = NormalizeOptional(request.EndpointKey);
        quota.BusinessKey = NormalizeOptional(request.BusinessKey);
        quota.ProducerKey = NormalizeOptional(request.ProducerKey);
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
