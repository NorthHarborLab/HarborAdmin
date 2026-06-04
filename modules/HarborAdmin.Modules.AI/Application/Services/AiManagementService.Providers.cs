using HarborAdmin.Modules.AI.Contracts.Dtos;
using HarborAdmin.Modules.AI.Contracts.Requests;
using HarborAdmin.Modules.AI.Domain.Entities;

namespace HarborAdmin.Modules.AI.Application.Services;

public sealed partial class AiManagementService
{
    /// <summary>
    /// 列出供应商。
    /// </summary>
    public async Task<IReadOnlyList<AiProviderDto>> ListProvidersAsync(CancellationToken cancellationToken = default)
        => (await repository.ListProvidersAsync(cancellationToken))
            .Select(provider => mapper.Map<AiProviderDto>(provider))
            .ToList();

    /// <summary>
    /// 保存供应商。
    /// </summary>
    public async Task<AiProviderDto> SaveProviderAsync(long? id, SaveAiProviderRequest request, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var provider = id is > 0
            ? await repository.GetProviderAsync(id.Value, cancellationToken) ?? throw new KeyNotFoundException($"AI provider '{id}' was not found.")
            : new AiProvider { CreatedAt = now };
        provider.ProviderKey = NormalizeKey(request.ProviderKey, nameof(request.ProviderKey));
        provider.DisplayName = NormalizeRequired(request.DisplayName, nameof(request.DisplayName));
        provider.AdapterType = NormalizeRequired(request.AdapterType, nameof(request.AdapterType));
        provider.BaseUrl = NormalizeRequired(request.BaseUrl, nameof(request.BaseUrl));
        provider.SecretRef = NormalizeOptional(request.SecretRef);
        provider.SecretVersion = await ResolveSecretVersionAsync(provider.SecretRef, cancellationToken);
        provider.DefaultHeadersJson = NormalizeOptional(request.DefaultHeadersJson);
        provider.DefaultBodyJson = NormalizeOptional(request.DefaultBodyJson);
        provider.Enabled = request.Enabled;
        provider.SupportsStreaming = request.SupportsStreaming;
        provider.TimeoutSeconds = request.TimeoutSeconds <= 0 ? 120 : request.TimeoutSeconds;
        provider.MaxRetryCount = Math.Max(0, request.MaxRetryCount);
        provider.CircuitBreakerFailureThreshold = request.CircuitBreakerFailureThreshold <= 0 ? 3 : request.CircuitBreakerFailureThreshold;
        provider.CircuitBreakerBreakSeconds = request.CircuitBreakerBreakSeconds <= 0 ? 60 : request.CircuitBreakerBreakSeconds;
        provider.UpdatedAt = now;
        var models = NormalizeProviderModels(request, provider, now);
        AiProvider saved;
        using var uow = unitOfWorkManager.Begin(entityRegistry.GetDbKey<AiProvider>());
        using (dbContext.Bind(uow.Orm))
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

    private async Task<int> ResolveSecretVersionAsync(string? secretRef, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(secretRef))
        {
            return 0;
        }

        var descriptor = await secretStore.GetAsync(secretRef, cancellationToken);
        if (descriptor is not { Enabled: true })
        {
            throw new ArgumentException($"SecretRef '{secretRef}' does not exist or is disabled.");
        }

        return descriptor.Version;
    }

    private static IReadOnlyList<AiProviderModel> NormalizeProviderModels(SaveAiProviderRequest request, AiProvider provider, DateTimeOffset now)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var models = request.Models
            .Where(m => !string.IsNullOrWhiteSpace(m.ModelName))
            .OrderBy(m => m.SortOrder)
            .Select((m, index) => new AiProviderModel
            {
                ProviderId = provider.Id,
                ModelName = NormalizeRequired(m.ModelName, nameof(m.ModelName)),
                DisplayName = NormalizeOptional(m.DisplayName),
                IsDefault = m.IsDefault,
                Enabled = m.Enabled,
                SupportsStreaming = m.SupportsStreaming,
                InputModalities = NormalizeOptional(m.InputModalities),
                OutputModalities = NormalizeOptional(m.OutputModalities),
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
            throw new ArgumentException("At least one provider model is required.", nameof(request.Models));
        }

        if (models.All(m => !m.IsDefault))
        {
            (models.FirstOrDefault(m => m.Enabled) ?? models[0]).IsDefault = true;
        }

        var defaultSeen = false;
        foreach (var model in models)
        {
            model.IsDefault = model.IsDefault && !defaultSeen;
            defaultSeen |= model.IsDefault;
        }

        return models;
    }

}
