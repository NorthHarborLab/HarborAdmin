using HarborAdmin.BuildingBlocks.Abstractions.Application;
using HarborAdmin.BuildingBlocks.Abstractions.Exception;
using HarborAdmin.BuildingBlocks.Abstractions.ModelResults;
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
public sealed class QuotaService(
    IAiQuotaRepository repository,
    IAiProviderRepository providerRepository,
    IAiModelQuotaRepository modelQuotaRepository,
    IHarborMapper mapper)
    : HarborApplicationPagedRepositoryService<AiModelQuota, AiModelQuotaDto, PageRequest, SaveAiModelQuotaRequest, IAiModelQuotaRepository>(modelQuotaRepository)
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
        _ = await providerRepository.GetAsync(providerId, cancellationToken) ?? throw new NotFoundDomainException($"AI provider '{providerId}' was not found.");
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

    /// <inheritdoc />
    protected override AiModelQuotaDto MapToDto(AiModelQuota entity) => mapper.Map<AiModelQuotaDto>(entity);

    /// <summary>
    /// 将保存请求应用到模型限额。
    /// </summary>
    protected override Task ApplySaveAsync(AiModelQuota entity, SaveAiModelQuotaRequest request, CancellationToken cancellationToken)
    {
        entity.ProviderKey = AiNormalizationHelper.NormalizeKey(request.ProviderKey, nameof(request.ProviderKey));
        entity.ModelName = AiNormalizationHelper.NormalizeOptional(request.ModelName);
        entity.EndpointKey = AiNormalizationHelper.NormalizeOptional(request.EndpointKey);
        entity.BusinessKey = AiNormalizationHelper.NormalizeOptional(request.BusinessKey);
        entity.ProducerKey = AiNormalizationHelper.NormalizeOptional(request.ProducerKey);
        entity.RequestsPerMinute = request.RequestsPerMinute;
        entity.TokensPerMinute = request.TokensPerMinute;
        entity.RequestsPerDay = request.RequestsPerDay;
        entity.TokensPerDay = request.TokensPerDay;
        entity.MonthlyBudget = request.MonthlyBudget;
        entity.Enabled = request.Enabled;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    protected override string GetNotFoundMessage(long id) => $"AI model quota '{id}' was not found.";
}
