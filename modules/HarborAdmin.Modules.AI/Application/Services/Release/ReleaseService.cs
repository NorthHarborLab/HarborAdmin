using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HarborAdmin.BuildingBlocks.Abstractions.Auth;
using HarborAdmin.BuildingBlocks.Abstractions.Exception;
using HarborAdmin.BuildingBlocks.EventBus;
using HarborAdmin.BuildingBlocks.Mapping;
using HarborAdmin.Modules.AI.Application.Abstractions;
using HarborAdmin.Modules.AI.Application.Mappings;
using HarborAdmin.Modules.AI.Contracts.Release.Dto;
using HarborAdmin.Modules.AI.Contracts.Release.Request;
using HarborAdmin.Modules.AI.Contracts.Shared.Constant;
using HarborAdmin.Modules.AI.Contracts.Shared.Dto;
using HarborAdmin.Modules.AI.Contracts.Shared.Snapshot;
using HarborAdmin.Modules.AI.Domain.Entities;

namespace HarborAdmin.Modules.AI.Application.Services.Release;

/// <summary>
/// AI 配置发布服务。
/// </summary>
public sealed class ReleaseService(
    IAiReleaseRepository releaseRepository,
    IAiQuotaRepository quotaRepository,
    IAiProviderRepository providerRepository,
    IAiBusinessRepository businessRepository,
    IAiPromptRepository promptRepository,
    IAiKnowledgeBaseRepository knowledgeBaseRepository,
    IAiModelQuotaRepository modelQuotaRepository,
    IEventPublisher eventPublisher,
    IHarborMapper mapper,
    ICurrentUser currentUser)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    /// <summary>
    /// 发布当前 AI 草稿配置。
    /// </summary>
    public async Task<AiReleaseDto> PublishAsync(PublishAiConfigRequest request, CancellationToken cancellationToken = default)
    {
        var releases = await releaseRepository.ListReleasesAsync(cancellationToken);
        var version = releases.Count == 0 ? 1 : releases.Max(r => r.Version) + 1;
        var snapshot = await BuildSnapshotAsync(version, cancellationToken);
        var snapshotJson = JsonSerializer.Serialize(snapshot, JsonOptions);
        var release = new AiConfigRelease
        {
            Version = version,
            SnapshotJson = snapshotJson,
            Checksum = Checksum(snapshotJson),
            PublishedBy = ResolvePublishedBy(currentUser),
            Remark = request.Remark?.Trim(),
            PublishedAt = DateTimeOffset.UtcNow
        };

        var created = await releaseRepository.InsertAndActivateReleaseAsync(release, cancellationToken);
        // 事务提交后再通知 Worker，确保订阅方读到的快照已经持久化。
        await PublishConfigChangedAsync(created, cancellationToken);
        return mapper.Map<AiReleaseDto>(created);
    }

    /// <summary>
    /// 回滚到指定发布版本。
    /// </summary>
    public async Task<AiReleaseDto> RollbackAsync(int version, CancellationToken cancellationToken = default)
    {
        var release = await releaseRepository.GetReleaseByVersionAsync(version, cancellationToken)
                      ?? throw new NotFoundDomainException($"AI release '{version}' was not found.");
        await releaseRepository.ActivateReleaseAsync(release.Id, cancellationToken);
        release.Active = true;
        await PublishConfigChangedAsync(release, cancellationToken);
        return mapper.Map<AiReleaseDto>(release);
    }

    /// <summary>
    /// 列出发布。
    /// </summary>
    public async Task<IReadOnlyList<AiReleaseDto>> ListReleasesAsync(CancellationToken cancellationToken = default) =>
        (await releaseRepository.ListReleasesAsync(cancellationToken))
        .Select(mapper.Map<AiReleaseDto>)
        .ToList();

    /// <summary>
    /// 获取已发布快照。
    /// </summary>
    public async Task<AiPublishedSnapshotDto?> GetPublishedAsync(int version = 0, CancellationToken cancellationToken = default)
    {
        var release = version > 0
            ? await releaseRepository.GetReleaseByVersionAsync(version, cancellationToken)
            : await releaseRepository.GetLatestReleaseAsync(cancellationToken);
        return release is null ? null : new AiPublishedSnapshotDto(release.Id, release.Version, release.Checksum, release.SnapshotJson, release.PublishedAt);
    }

    /// <summary>
    /// 从当前启用的草稿配置构建运行时快照。
    /// </summary>
    private async Task<AiConfigSnapshot> BuildSnapshotAsync(int version, CancellationToken cancellationToken)
    {
        var providers = (await providerRepository.ListAsync(cancellationToken)).Where(p => p.Enabled).ToList();
        var businesses = (await businessRepository.ListAsync(cancellationToken)).Where(b => b.Enabled).ToList();
        var prompts = (await promptRepository.ListAsync(cancellationToken)).Where(p => p.Enabled).ToList();
        var knowledgeBases = (await knowledgeBaseRepository.ListAsync(cancellationToken)).Where(k => k.Enabled).ToList();
        var providerQuotas = (await quotaRepository.ListProviderQuotasAsync(cancellationToken)).Where(q => q.Enabled).ToList();
        var modelQuotas = (await modelQuotaRepository.ListAsync(cancellationToken)).Where(q => q.Enabled).ToList();
        var providerKeys = providers.ToDictionary(p => p.Id, p => p.ProviderKey);
        return new AiConfigSnapshot(
            version,
            providers.Select(mapper.Map<AiProviderSnapshot>).ToList(),
            businesses.Select(mapper.Map<AiBusinessSnapshot>).ToList(),
            prompts.Select(mapper.Map<AiPromptSnapshot>).ToList(),
            knowledgeBases.Select(mapper.Map<AiKnowledgeSnapshot>).ToList(),
            providerQuotas
                // 供应商已禁用或被删除时，对应限额不进入运行时快照。
                .Where(quota => providerKeys.ContainsKey(quota.ProviderId))
                .Select(quota => mapper.Map<AiProviderQuotaSnapshot>(new AiProviderQuotaSnapshotSource(quota, providerKeys[quota.ProviderId])))
                .ToList(),
            modelQuotas.Select(mapper.Map<AiModelQuotaSnapshot>).ToList());
    }

    /// <summary>
    /// 发布配置变更事件。
    /// </summary>
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

    /// <summary>
    /// 计算发布快照校验和。
    /// </summary>
    private static string Checksum(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    /// <summary>
    /// 解析当前登录用户作为发布人。
    /// </summary>
    private static string? ResolvePublishedBy(ICurrentUser currentUser)
    {
        if (!string.IsNullOrWhiteSpace(currentUser.DisplayName))
        {
            return currentUser.DisplayName.Trim();
        }

        if (!string.IsNullOrWhiteSpace(currentUser.UserName))
        {
            return currentUser.UserName.Trim();
        }

        return currentUser.Id > 0 ? currentUser.Id.ToString() : null;
    }
}
