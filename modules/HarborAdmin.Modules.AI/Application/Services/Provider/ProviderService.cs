using HarborAdmin.BuildingBlocks.Abstractions.Exception;
using HarborAdmin.BuildingBlocks.Abstractions.Secrets;
using HarborAdmin.BuildingBlocks.Mapping;
using HarborAdmin.Modules.AI.Application.Abstractions;
using HarborAdmin.Modules.AI.Application.Services.Shared;
using HarborAdmin.Modules.AI.Contracts.Provider.Dto;
using HarborAdmin.Modules.AI.Contracts.Provider.Request;
using HarborAdmin.Modules.AI.Domain.Entities;

namespace HarborAdmin.Modules.AI.Application.Services.Provider;

/// <summary>
/// AI 供应商管理服务。
/// </summary>
public sealed class ProviderService(IAiRepository repository, AiServiceContext context, ISecretStore secretStore, IHarborMapper mapper)
{
    /// <summary>
    /// 列出供应商。
    /// </summary>
    public async Task<IReadOnlyList<AiProviderDto>> ListProvidersAsync(CancellationToken cancellationToken = default)
        => (await repository.ListProvidersAsync(cancellationToken))
            .Select(mapper.Map<AiProviderDto>)
            .ToList();

    /// <summary>
    /// 保存供应商。
    /// </summary>
    public async Task<AiProviderDto> SaveProviderAsync(long? id, SaveAiProviderRequest request, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var provider = id is > 0
            ? await repository.GetProviderAsync(id.Value, cancellationToken) ?? throw new NotFoundDomainException($"AI provider '{id}' was not found.")
            : new AiProvider { CreatedAt = now };
        provider.ProviderKey = AiNormalizationHelper.NormalizeKey(request.ProviderKey, nameof(request.ProviderKey));
        provider.DisplayName = AiNormalizationHelper.NormalizeRequired(request.DisplayName, nameof(request.DisplayName));
        provider.AdapterType = AiNormalizationHelper.NormalizeRequired(request.AdapterType, nameof(request.AdapterType));
        provider.BaseUrl = AiNormalizationHelper.NormalizeRequired(request.BaseUrl, nameof(request.BaseUrl));
        provider.SecretRef = AiNormalizationHelper.NormalizeOptional(request.SecretRef);
        provider.SecretVersion = await ResolveSecretVersionAsync(provider.SecretRef, cancellationToken);
        provider.DefaultHeadersJson = AiNormalizationHelper.NormalizeOptional(request.DefaultHeadersJson);
        provider.DefaultBodyJson = AiNormalizationHelper.NormalizeOptional(request.DefaultBodyJson);
        provider.Enabled = request.Enabled;
        provider.SupportsStreaming = request.SupportsStreaming;
        provider.TimeoutSeconds = request.TimeoutSeconds <= 0 ? 120 : request.TimeoutSeconds;
        provider.MaxRetryCount = Math.Max(0, request.MaxRetryCount);
        provider.CircuitBreakerFailureThreshold = request.CircuitBreakerFailureThreshold <= 0 ? 3 : request.CircuitBreakerFailureThreshold;
        provider.CircuitBreakerBreakSeconds = request.CircuitBreakerBreakSeconds <= 0 ? 60 : request.CircuitBreakerBreakSeconds;
        provider.UpdatedAt = now;
        var models = NormalizeProviderModels(request, provider, now);
        AiProvider saved;
        // 供应商和模型列表必须在同一工作单元内保存，避免只更新供应商或只更新模型。
        using var uow = context.UnitOfWorkManager.Begin(context.EntityRegistry.GetDbKey<AiProvider>());
        using (context.DbContext.Bind(uow.Orm))
        {
            saved = await repository.SaveProviderAsync(provider, models, cancellationToken);
        }

        uow.Commit();
        return mapper.Map<AiProviderDto>(saved);
    }

    /// <summary>
    /// 删除供应商。
    /// </summary>
    public Task DeleteProviderAsync(long id, CancellationToken cancellationToken = default) =>
        repository.DeleteProviderAsync(id, cancellationToken);

    /// <summary>
    /// 校验 Secret 引用并固定当前版本号。
    /// </summary>
    private async Task<int> ResolveSecretVersionAsync(string? secretRef, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(secretRef))
        {
            return 0;
        }

        var descriptor = await secretStore.GetAsync(secretRef, cancellationToken);
        if (descriptor is not { Enabled: true })
        {
            throw new ValidationDomainException($"SecretRef '{secretRef}' does not exist or is disabled.");
        }

        // 发布快照只保存 SecretRef 和版本号，固定版本可避免后续轮换影响历史发布。
        return descriptor.Version;
    }

    /// <summary>
    /// 规范化供应商模型列表并保证至少一个默认模型。
    /// </summary>
    private static IReadOnlyList<AiProviderModel> NormalizeProviderModels(SaveAiProviderRequest request, AiProvider provider, DateTimeOffset now)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var models = request.Models
            .Where(m => !string.IsNullOrWhiteSpace(m.ModelName))
            .OrderBy(m => m.SortOrder)
            .Select((m, index) => new AiProviderModel
            {
                ProviderId = provider.Id,
                ModelName = AiNormalizationHelper.NormalizeRequired(m.ModelName, nameof(m.ModelName)),
                DisplayName = AiNormalizationHelper.NormalizeOptional(m.DisplayName),
                IsDefault = m.IsDefault,
                Enabled = m.Enabled,
                SupportsStreaming = m.SupportsStreaming,
                InputModalities = AiNormalizationHelper.NormalizeOptional(m.InputModalities),
                OutputModalities = AiNormalizationHelper.NormalizeOptional(m.OutputModalities),
                SupportsVision = m.SupportsVision,
                SupportsTools = m.SupportsTools,
                SupportsStructuredOutput = m.SupportsStructuredOutput,
                SupportsJsonMode = m.SupportsJsonMode,
                SupportsReasoning = m.SupportsReasoning,
                ContextWindow = m.ContextWindow,
                MaxOutputTokens = m.MaxOutputTokens,
                InputPrice = m.InputPrice,
                OutputPrice = m.OutputPrice,
                CachedInputPrice = m.CachedInputPrice,
                ReasoningPrice = m.ReasoningPrice,
                SortOrder = m.SortOrder <= 0 ? index + 1 : m.SortOrder,
                CreatedAt = now,
                UpdatedAt = now
            })
            .Where(m => seen.Add(m.ModelName))
            .ToList();
        if (models.Count == 0)
        {
            throw new ValidationDomainException("At least one provider model is required.");
        }

        if (models.All(m => !m.IsDefault))
        {
            // 前端未指定默认模型时，优先选择启用模型，否则退回第一项，保证运行时可解析默认路由。
            (models.FirstOrDefault(m => m.Enabled) ?? models[0]).IsDefault = true;
        }

        var defaultSeen = false;
        foreach (var model in models)
        {
            // 多个默认模型只保留排序后的第一个，避免 Worker 侧默认模型选择不确定。
            model.IsDefault = model.IsDefault && !defaultSeen;
            defaultSeen |= model.IsDefault;
        }

        return models;
    }
}
