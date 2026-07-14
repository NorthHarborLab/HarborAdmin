using HarborAdmin.BuildingBlocks.Application;
using HarborAdmin.BuildingBlocks.Abstractions.Exception;
using HarborAdmin.BuildingBlocks.Abstractions.ModelResults;
using HarborAdmin.BuildingBlocks.Abstractions.Repositories;
using HarborAdmin.BuildingBlocks.Abstractions.Results;
using HarborAdmin.BuildingBlocks.Abstractions.Secrets;
using HarborAdmin.BuildingBlocks.Mapping;
using HarborAdmin.Modules.AI.Application.Abstractions;
using HarborAdmin.Modules.AI.Application.Services.Shared;
using HarborAdmin.Modules.AI.Contracts.Provider.Dto;
using HarborAdmin.Modules.AI.Contracts.Provider.Request;
using HarborAdmin.Modules.AI.Contracts.Shared.ErrorCode;
using HarborAdmin.Modules.AI.Domain.Entities;

namespace HarborAdmin.Modules.AI.Application.Services.Provider;

/// <summary>
/// AI 供应商管理服务。
/// </summary>
public sealed class ProviderService(IAiProviderRepository repository, ISecretStore secretStore, IHarborMapper mapper)
    : HarborCrudApplicationService<AiProvider, AiProviderDto, PageRequest, SaveAiProviderRequest, IAiProviderRepository>(repository)
{
    /// <inheritdoc />
    protected override AiProviderDto MapToDto(AiProvider entity) => mapper.Map<AiProviderDto>(entity);

    /// <inheritdoc />
    protected override AiProvider CreateEntity(SaveAiProviderRequest request) => new() { CreatedAt = UtcNow };

    /// <summary>
    /// 将保存请求应用到供应商。
    /// </summary>
    protected override async Task<HarborResult> ApplySaveAsync(AiProvider entity, SaveAiProviderRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var now = UtcNow;
            entity.ProviderKey = AiNormalizationHelper.NormalizeKey(request.ProviderKey, nameof(request.ProviderKey));
            entity.DisplayName = AiNormalizationHelper.NormalizeRequired(request.DisplayName, nameof(request.DisplayName));
            entity.AdapterType = AiNormalizationHelper.NormalizeRequired(request.AdapterType, nameof(request.AdapterType));
            entity.BaseUrl = AiNormalizationHelper.NormalizeRequired(request.BaseUrl, nameof(request.BaseUrl));
            entity.SecretRef = AiNormalizationHelper.NormalizeOptional(request.SecretRef);
            entity.DefaultHeadersJson = AiNormalizationHelper.NormalizeOptional(request.DefaultHeadersJson);
            entity.DefaultBodyJson = AiNormalizationHelper.NormalizeOptional(request.DefaultBodyJson);
            entity.Enabled = request.Enabled;
            entity.SupportsStreaming = request.SupportsStreaming;
            entity.TimeoutSeconds = request.TimeoutSeconds <= 0 ? 120 : request.TimeoutSeconds;
            entity.MaxRetryCount = Math.Max(0, request.MaxRetryCount);
            entity.CircuitBreakerFailureThreshold = request.CircuitBreakerFailureThreshold <= 0 ? 3 : request.CircuitBreakerFailureThreshold;
            entity.CircuitBreakerBreakSeconds = request.CircuitBreakerBreakSeconds <= 0 ? 60 : request.CircuitBreakerBreakSeconds;
            entity.UpdatedAt = now;

            if (await Repository.ProviderKeyExistsAsync(
                    entity.ProviderKey,
                    entity.Id > 0 ? entity.Id : null,
                    cancellationToken))
            {
                return HarborResult.Failure(AiProviderErrorCodes.DuplicateKey.Create(
                    new Dictionary<string, object?> { ["providerKey"] = entity.ProviderKey }));
            }

            var secretVersion = await ResolveSecretVersionAsync(entity.SecretRef, cancellationToken);
            if (!secretVersion.IsSuccess)
            {
                return HarborResult.Failure(secretVersion.Error!);
            }

            var models = NormalizeProviderModels(request, entity, now);
            if (!models.IsSuccess)
            {
                return HarborResult.Failure(models.Error!);
            }

            entity.SecretVersion = secretVersion.Value;
            entity.Models = models.Value!.ToList();
            return HarborResult.Success();
        }
        catch (ValidationDomainException exception)
        {
            return HarborResult.Failure(AiProviderErrorCodes.InvalidInput.Create(
                new Dictionary<string, object?> { ["reason"] = exception.Message }, exception.Errors, exception.ErrorMeta));
        }
    }

    /// <inheritdoc />
    protected override HarborErrorDefinition NotFoundError => AiProviderErrorCodes.NotFound;

    /// <summary>
    /// 校验 Secret 引用并固定当前版本号。
    /// </summary>
    private async Task<HarborResult<int>> ResolveSecretVersionAsync(string? secretRef, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(secretRef))
        {
            return HarborResult<int>.Success(0);
        }

        var descriptor = await secretStore.GetAsync(secretRef, cancellationToken);
        if (descriptor is not { Enabled: true })
        {
            return HarborResult<int>.Failure(AiProviderErrorCodes.SecretUnavailable.Create(
                new Dictionary<string, object?> { ["secretRef"] = secretRef }));
        }

        // 发布快照只保存 SecretRef 和版本号，固定版本可避免后续轮换影响历史发布。
        return HarborResult<int>.Success(descriptor.Version);
    }

    /// <summary>
    /// 规范化供应商模型列表并保证至少一个默认模型。
    /// </summary>
    private static HarborResult<IReadOnlyList<AiProviderModel>> NormalizeProviderModels(
        SaveAiProviderRequest request,
        AiProvider provider,
        DateTimeOffset now)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var models = request.Models
            .Where(model => !string.IsNullOrWhiteSpace(model.ModelName))
            .OrderBy(model => model.SortOrder)
            .Select((model, index) => new AiProviderModel
            {
                ProviderId = provider.Id,
                ModelName = AiNormalizationHelper.NormalizeRequired(model.ModelName, nameof(model.ModelName)),
                DisplayName = AiNormalizationHelper.NormalizeOptional(model.DisplayName),
                IsDefault = model.IsDefault,
                Enabled = model.Enabled,
                SupportsStreaming = model.SupportsStreaming,
                InputModalities = AiNormalizationHelper.NormalizeOptional(model.InputModalities),
                OutputModalities = AiNormalizationHelper.NormalizeOptional(model.OutputModalities),
                SupportsVision = model.SupportsVision,
                SupportsTools = model.SupportsTools,
                SupportsStructuredOutput = model.SupportsStructuredOutput,
                SupportsJsonMode = model.SupportsJsonMode,
                SupportsReasoning = model.SupportsReasoning,
                ContextWindow = model.ContextWindow,
                MaxOutputTokens = model.MaxOutputTokens,
                InputPrice = model.InputPrice,
                OutputPrice = model.OutputPrice,
                CachedInputPrice = model.CachedInputPrice,
                ReasoningPrice = model.ReasoningPrice,
                SortOrder = model.SortOrder <= 0 ? index + 1 : model.SortOrder,
                CreatedAt = now,
                UpdatedAt = now
            })
            .Where(model => seen.Add(model.ModelName))
            .ToList();
        if (models.Count == 0)
        {
            return HarborResult<IReadOnlyList<AiProviderModel>>.Failure(AiProviderErrorCodes.ModelRequired.Create());
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

        return HarborResult<IReadOnlyList<AiProviderModel>>.Success(models);
    }
}
