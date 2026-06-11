using HarborAdmin.BuildingBlocks.Abstractions.Application;
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
public sealed class ProviderService(IAiProviderRepository repository, ISecretStore secretStore, IHarborMapper mapper)
    : HarborApplicationRepositoryService<AiProvider, AiProviderDto, SaveAiProviderRequest, IAiProviderRepository>(repository)
{
    /// <inheritdoc />
    protected override AiProviderDto MapToDto(AiProvider entity) => mapper.Map<AiProviderDto>(entity);

    /// <inheritdoc />
    protected override AiProvider CreateEntity(SaveAiProviderRequest request) => new() { CreatedAt = UtcNow };

    /// <summary>
    /// 将保存请求应用到供应商。
    /// </summary>
    protected override async Task ApplySaveAsync(AiProvider entity, SaveAiProviderRequest request, CancellationToken cancellationToken)
    {
        var now = UtcNow;
        entity.ProviderKey = AiNormalizationHelper.NormalizeKey(request.ProviderKey, nameof(request.ProviderKey));
        entity.DisplayName = AiNormalizationHelper.NormalizeRequired(request.DisplayName, nameof(request.DisplayName));
        entity.AdapterType = AiNormalizationHelper.NormalizeRequired(request.AdapterType, nameof(request.AdapterType));
        entity.BaseUrl = AiNormalizationHelper.NormalizeRequired(request.BaseUrl, nameof(request.BaseUrl));
        entity.SecretRef = AiNormalizationHelper.NormalizeOptional(request.SecretRef);
        entity.SecretVersion = await ResolveSecretVersionAsync(entity.SecretRef, cancellationToken);
        entity.DefaultHeadersJson = AiNormalizationHelper.NormalizeOptional(request.DefaultHeadersJson);
        entity.DefaultBodyJson = AiNormalizationHelper.NormalizeOptional(request.DefaultBodyJson);
        entity.Enabled = request.Enabled;
        entity.SupportsStreaming = request.SupportsStreaming;
        entity.TimeoutSeconds = request.TimeoutSeconds <= 0 ? 120 : request.TimeoutSeconds;
        entity.MaxRetryCount = Math.Max(0, request.MaxRetryCount);
        entity.CircuitBreakerFailureThreshold = request.CircuitBreakerFailureThreshold <= 0 ? 3 : request.CircuitBreakerFailureThreshold;
        entity.CircuitBreakerBreakSeconds = request.CircuitBreakerBreakSeconds <= 0 ? 60 : request.CircuitBreakerBreakSeconds;
        entity.UpdatedAt = now;
        entity.Models = NormalizeProviderModels(request, entity, now).ToList();
    }

    /// <inheritdoc />
    protected override string GetNotFoundMessage(long id) => $"AI provider '{id}' was not found.";

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