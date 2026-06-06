using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HarborAdmin.BuildingBlocks.Abstractions.Exception;
using HarborAdmin.Modules.AI.Contracts.Constants;
using HarborAdmin.Modules.AI.Contracts.Dtos;
using HarborAdmin.Modules.AI.Contracts.Snapshots;
using HarborAdmin.Modules.AI.Domain.Entities;

namespace HarborAdmin.Modules.AI.Application.Services;

public sealed partial class AiManagementService
{
    /// <summary>
    /// 发布当前 AI 草稿配置。
    /// </summary>
    public async Task<AiReleaseDto> PublishAsync(PublishAiConfigRequest request, CancellationToken cancellationToken = default)
    {
        var releases = await repository.ListReleasesAsync(cancellationToken);
        var version = releases.Count == 0 ? 1 : releases.Max(r => r.Version) + 1;
        var snapshot = await BuildSnapshotAsync(version, cancellationToken);
        var snapshotJson = JsonSerializer.Serialize(snapshot, JsonOptions);
        var release = new AiConfigRelease
        {
            Version = version,
            SnapshotJson = snapshotJson,
            Checksum = Checksum(snapshotJson),
            PublishedBy = request.PublishedBy?.Trim(),
            Remark = request.Remark?.Trim(),
            PublishedAt = DateTimeOffset.UtcNow
        };

        AiConfigRelease created;
        using var uow = unitOfWorkManager.Begin(entityRegistry.GetDbKey<AiConfigRelease>());
        using (dbContext.Bind(uow.Orm))
        {
            created = await repository.InsertReleaseAsync(release, cancellationToken);
            await repository.ActivateReleaseAsync(created.Id, cancellationToken);
            created.Active = true;
        }

        uow.Commit();
        await PublishConfigChangedAsync(created, cancellationToken);
        return mapper.Map<AiReleaseDto>(created);
    }

    /// <summary>
    /// 回滚到指定发布版本。
    /// </summary>
    public async Task<AiReleaseDto> RollbackAsync(int version, CancellationToken cancellationToken = default)
    {
        var release = await repository.GetReleaseByVersionAsync(version, cancellationToken)
                      ?? throw new NotFoundDomainException($"AI release '{version}' was not found.");
        using var uow = unitOfWorkManager.Begin(entityRegistry.GetDbKey<AiConfigRelease>());
        using (dbContext.Bind(uow.Orm))
        {
            await repository.ActivateReleaseAsync(release.Id, cancellationToken);
            release.Active = true;
        }

        uow.Commit();
        await PublishConfigChangedAsync(release, cancellationToken);
        return mapper.Map<AiReleaseDto>(release);
    }

    /// <summary>
    /// 列出发布。
    /// </summary>
    public async Task<IReadOnlyList<AiReleaseDto>> ListReleasesAsync(CancellationToken cancellationToken = default) =>
        (await repository.ListReleasesAsync(cancellationToken))
        .Select(release => mapper.Map<AiReleaseDto>(release))
        .ToList();

    /// <summary>
    /// 获取已发布快照。
    /// </summary>
    public async Task<AiPublishedSnapshotDto?> GetPublishedAsync(int version = 0, CancellationToken cancellationToken = default)
    {
        var release = version > 0
            ? await repository.GetReleaseByVersionAsync(version, cancellationToken)
            : await repository.GetLatestReleaseAsync(cancellationToken);
        return release is null ? null : new AiPublishedSnapshotDto(release.Id, release.Version, release.Checksum, release.SnapshotJson, release.PublishedAt);
    }

    private async Task<AiConfigSnapshot> BuildSnapshotAsync(int version, CancellationToken cancellationToken)
    {
        var providers = (await repository.ListProvidersAsync(cancellationToken)).Where(p => p.Enabled).ToList();
        var businesses = (await repository.ListBusinessesAsync(cancellationToken)).Where(b => b.Enabled).ToList();
        var prompts = (await repository.ListPromptsAsync(cancellationToken)).Where(p => p.Enabled).ToList();
        var knowledgeBases = (await repository.ListKnowledgeBasesAsync(cancellationToken)).Where(k => k.Enabled).ToList();
        var providerQuotas = (await repository.ListProviderQuotasAsync(cancellationToken)).Where(q => q.Enabled).ToList();
        var modelQuotas = (await repository.ListModelQuotasAsync(cancellationToken)).Where(q => q.Enabled).ToList();
        var providerKeys = providers.ToDictionary(p => p.Id, p => p.ProviderKey);
        return new AiConfigSnapshot(
            version,
            providers.Select(ToSnapshot).ToList(),
            businesses.Select(ToSnapshot).ToList(),
            prompts.Select(ToSnapshot).ToList(),
            knowledgeBases.Select(ToSnapshot).ToList(),
            providerQuotas.Where(q => providerKeys.ContainsKey(q.ProviderId)).Select(q => ToSnapshot(q, providerKeys[q.ProviderId])).ToList(),
            modelQuotas.Select(ToSnapshot).ToList());
    }

    private async Task PublishConfigChangedAsync(AiConfigRelease release, CancellationToken cancellationToken)
    {
        try
        {
            await eventPublisher.PublishAsync(AiEventTopics.ConfigPublished, new AiConfigPublishedEvent(release.Id, release.Version, release.Checksum),
                cancellationToken);
        }
        catch
        {
            // 发布快照已经提交，通知失败不回滚。
        }
    }

    private static AiProviderSnapshot ToSnapshot(AiProvider provider) =>
        new(provider.ProviderKey, provider.DisplayName, provider.AdapterType, provider.BaseUrl, provider.SecretRef, provider.SecretVersion,
            provider.DefaultHeadersJson, provider.DefaultBodyJson, provider.SupportsStreaming, provider.TimeoutSeconds, provider.MaxRetryCount,
            provider.CircuitBreakerFailureThreshold, provider.CircuitBreakerBreakSeconds,
            provider.Models.Where(m => m.Enabled).OrderBy(m => m.SortOrder).Select(ToSnapshot).ToList());

    private static AiProviderModelSnapshot ToSnapshot(AiProviderModel model) =>
        new(model.ModelName, model.IsDefault, model.SupportsStreaming, model.SupportsVision, model.SupportsTools, model.SupportsStructuredOutput,
            model.SupportsJsonMode, model.ContextWindow, model.MaxOutputTokens, model.InputPrice, model.OutputPrice, model.CachedInputPrice,
            model.ReasoningPrice);

    private static AiBusinessSnapshot ToSnapshot(AiBusiness business) =>
        new(business.BusinessKey, business.Name, business.AllowedProducerKeys, business.SigningSecretRef, business.CallbackTopic, business.PromptKey,
            business.KnowledgeKeys, business.EnableStreaming, business.AllowKnowledgeTextAppend, business.AllowKnowledgeTextOverride,
            business.MaxContextTokens, business.ContextOverflowStrategy, business.FailureStrategy, business.AllowModelOverride, business.AllowPromptOverride,
            business.AllowKnowledgeText, business.AllowProviderOptionsOverride, business.AllowToolOptionsOverride, business.OutputFormat,
            business.OutputJsonSchema, business.OutputStrict, business.OutputValidateAndRetry, business.OutputMaxRetryCount, business.ToolOptionsJson,
            business.MaxToolRounds, business.ProviderOptionsJson, business.Routes.Where(r => r.Enabled).OrderBy(r => r.Priority).Select(ToSnapshot).ToList());

    private static AiBusinessRouteSnapshot ToSnapshot(AiBusinessProviderRoute route) =>
        new(route.ProviderKey, route.ModelOverride, route.Priority, route.ProviderOptionsJson, route.OpenRouterOptionsJson);

    private static AiPromptSnapshot ToSnapshot(AiPrompt prompt) =>
        new(prompt.PromptKey, prompt.Version, prompt.SystemPromptMarkdown, prompt.UserPromptMarkdown, prompt.VariablesJson);

    private static AiKnowledgeSnapshot ToSnapshot(AiKnowledgeBase knowledgeBase) =>
        new(knowledgeBase.KnowledgeKey, knowledgeBase.Name, knowledgeBase.ContentMarkdown, knowledgeBase.RetrievalType, knowledgeBase.RetrievalOptionsJson,
            knowledgeBase.AppendReferences);

    private static AiProviderQuotaSnapshot ToSnapshot(AiProviderQuota quota, string providerKey) =>
        new(providerKey, quota.ProducerKey, quota.RequestsPerMinute, quota.RequestsPerDay, quota.TokensPerDay, quota.TokensPerMonth, quota.MonthlyBudget);

    private static AiModelQuotaSnapshot ToSnapshot(AiModelQuota quota) =>
        new(quota.ProviderKey, quota.ModelName, quota.BusinessKey, quota.ProducerKey, quota.RequestsPerMinute, quota.TokensPerMinute,
            quota.RequestsPerDay, quota.TokensPerDay, quota.MonthlyBudget);

    private static string Checksum(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
