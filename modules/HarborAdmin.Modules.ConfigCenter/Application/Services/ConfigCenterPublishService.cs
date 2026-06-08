using HarborAdmin.BuildingBlocks.Abstractions.Api;
using HarborAdmin.BuildingBlocks.Abstractions.Exception;
using HarborAdmin.BuildingBlocks.Data;
using HarborAdmin.BuildingBlocks.Mapping;
using HarborAdmin.Modules.ConfigCenter.Application.Abstractions;
using HarborAdmin.Modules.ConfigCenter.Contracts.Dtos;
using HarborAdmin.Modules.ConfigCenter.Contracts.Requests;
using HarborAdmin.Modules.ConfigCenter.Domain.Entities;
using HarborAdmin.Modules.ConfigCenter.Infrastructure.Contexts;

namespace HarborAdmin.Modules.ConfigCenter.Application.Services;

/// <summary>
/// 配置中心发布服务。
/// </summary>
public sealed class ConfigCenterPublishService(
    IConfigCenterRepository repository,
    IConfigCenterDbContext dbContext,
    DbEntityRegistry entityRegistry,
    UnitOfWorkManagerCloud unitOfWorkManager,
    IConfigCenterNotifyClient notifyClient,
    ConfigCenterApplicationService applicationService,
    ConfigCenterSnapshotService snapshotService,
    ConfigSecretReferenceValidator secretValidator,
    IHarborMapper mapper)
{
    /// <summary>
    /// 列出发布历史。
    /// </summary>
    public async Task<IReadOnlyList<ConfigReleaseDto>> ListReleasesAsync(string appId, CancellationToken cancellationToken = default)
    {
        await applicationService.RequireApplicationAsync(appId, cancellationToken);
        var releases = await repository.ListReleasesAsync(appId.Trim(), cancellationToken);
        return releases.Select(release => mapper.Map<ConfigReleaseDto>(release)).ToList();
    }

    /// <summary>
    /// 发布当前草稿。
    /// </summary>
    public async Task<PublishConfigResult> PublishAsync(string appId, PublishConfigRequest request,
        CancellationToken cancellationToken = default)
    {
        await applicationService.RequireApplicationAsync(appId, cancellationToken);
        var normalizedAppId = appId.Trim();

        var draftItems = await repository.ListItemsAsync(normalizedAppId, cancellationToken);
        var latest = await repository.GetLatestReleaseAsync(normalizedAppId, cancellationToken);
        var nextVersion = (latest?.Version ?? 0) + 1;

        var release = new ConfigRelease
        {
            AppId = normalizedAppId,
            Version = nextVersion,
            PublishedBy = request.PublishedBy?.Trim(),
            PublishedAt = DateTimeOffset.UtcNow
        };

        var releaseItems = new List<ConfigReleaseItem>(draftItems.Count);
        foreach (var item in draftItems)
        {
            releaseItems.Add(new ConfigReleaseItem
            {
                Group = item.Group,
                Key = item.Key,
                Value = await secretValidator.PinSecretReferencesAsync(item.Value, item.ValueType, cancellationToken),
                ValueType = item.ValueType
            });
        }

        ConfigRelease created;
        using var uow = unitOfWorkManager.Begin(entityRegistry.GetDbKey<ConfigRelease>());
        using (dbContext.Bind(uow.Orm))
        {
            created = await repository.InsertReleaseAsync(release, releaseItems, cancellationToken);
        }

        uow.Commit();
        await notifyClient.NotifyPublishedAsync(normalizedAppId, created.Id, cancellationToken);
        return new PublishConfigResult(created.Id, created.Version);
    }

    /// <summary>
    /// 获取已发布配置快照。
    /// </summary>
    public Task<PublishedConfigSnapshot?> GetPublishedSnapshotAsync(string appId, int version = 0,
        CancellationToken cancellationToken = default) =>
        snapshotService.GetPublishedSnapshotAsync(appId, version, cancellationToken);

    /// <summary>
    /// 获取已解析 Secret 的发布快照。
    /// </summary>
    public Task<PublishedConfigSnapshot?> GetResolvedPublishedSnapshotAsync(string appId, int version = 0,
        CancellationToken cancellationToken = default) =>
        snapshotService.GetResolvedPublishedSnapshotAsync(appId, version, cancellationToken);

    /// <summary>
    /// 按发布主键获取配置快照。
    /// </summary>
    public Task<PublishedConfigSnapshot> GetPublishedSnapshotByReleaseIdAsync(long releaseId, CancellationToken cancellationToken = default) =>
        snapshotService.GetPublishedSnapshotByReleaseIdAsync(releaseId, cancellationToken);

    /// <summary>
    /// 按发布主键获取已解析 Secret 的配置快照。
    /// </summary>
    public Task<PublishedConfigSnapshot> GetResolvedPublishedSnapshotByReleaseIdAsync(long releaseId,
        CancellationToken cancellationToken = default) =>
        snapshotService.GetResolvedPublishedSnapshotByReleaseIdAsync(releaseId, cancellationToken);
}
