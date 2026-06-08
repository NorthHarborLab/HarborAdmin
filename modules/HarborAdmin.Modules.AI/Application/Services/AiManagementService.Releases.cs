using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HarborAdmin.BuildingBlocks.Abstractions.Exception;
using HarborAdmin.Modules.AI.Contracts.Constants;
using HarborAdmin.Modules.AI.Application.Mappings;
using HarborAdmin.Modules.AI.Contracts.Dtos;
using HarborAdmin.Modules.AI.Contracts.Requests;
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
            providers.Select(provider => mapper.Map<AiProviderSnapshot>(provider)).ToList(),
            businesses.Select(business => mapper.Map<AiBusinessSnapshot>(business)).ToList(),
            prompts.Select(prompt => mapper.Map<AiPromptSnapshot>(prompt)).ToList(),
            knowledgeBases.Select(knowledge => mapper.Map<AiKnowledgeSnapshot>(knowledge)).ToList(),
            providerQuotas
                .Where(quota => providerKeys.ContainsKey(quota.ProviderId))
                .Select(quota => mapper.Map<AiProviderQuotaSnapshot>(new AiProviderQuotaSnapshotSource(quota, providerKeys[quota.ProviderId])))
                .ToList(),
            modelQuotas.Select(quota => mapper.Map<AiModelQuotaSnapshot>(quota)).ToList());
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

    private static string Checksum(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
