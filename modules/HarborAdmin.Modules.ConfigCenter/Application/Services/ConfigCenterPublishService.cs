using HarborAdmin.BuildingBlocks.Mapping;
using HarborAdmin.Modules.ConfigCenter.Contracts.Publish.Request;

namespace HarborAdmin.Modules.ConfigCenter.Application.Services;

/// <summary>
/// 配置中心发布服务。
/// </summary>
public sealed class ConfigCenterPublishService(
    IConfigCenterRepository repository,
    IConfigItemRepository itemRepository,
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
    public async Task<PublishConfigResult> PublishAsync(string appId, PublishConfigRequest request, CancellationToken cancellationToken = default)
    {
        await applicationService.RequireApplicationAsync(appId, cancellationToken);
        var normalizedAppId = appId.Trim();

        var draftItems = await itemRepository.ListByAppIdAsync(normalizedAppId, cancellationToken);
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
            // 发布快照保存的是固定后的 Secret 标记，运行时读取 resolved 快照时才解析明文。
            releaseItems.Add(new ConfigReleaseItem
            {
                Group = item.Group,
                Key = item.Key,
                Value = await secretValidator.PinSecretReferencesAsync(item.Value, item.ValueType, cancellationToken),
                ValueType = item.ValueType
            });
        }

        var created = await repository.InsertReleaseAsync(release, releaseItems, cancellationToken);
        // 事务提交后再通知 TCP 读进程刷新缓存，避免读进程提前看到未提交数据。
        await notifyClient.NotifyPublishedAsync(normalizedAppId, created.Id, cancellationToken);
        return new PublishConfigResult(created.Id, created.Version);
    }

    /// <summary>
    /// 获取已发布配置快照。
    /// </summary>
    public Task<PublishedConfigSnapshot?> GetPublishedSnapshotAsync(string appId, int version = 0, CancellationToken cancellationToken = default) =>
        snapshotService.GetPublishedSnapshotAsync(appId, version, cancellationToken);

    /// <summary>
    /// 获取已发布配置快照，不存在时抛出 <see cref="NotFoundDomainException"/>。
    /// </summary>
    public async Task<PublishedConfigSnapshot> GetPublishedSnapshotRequiredAsync(string appId, int version = 0, CancellationToken cancellationToken = default) =>
        await GetPublishedSnapshotAsync(appId, version, cancellationToken)
        ?? throw new NotFoundDomainException("Published snapshot not found.");

    /// <summary>
    /// 获取已解析 Secret 的发布快照。
    /// </summary>
    public Task<PublishedConfigSnapshot?> GetResolvedPublishedSnapshotAsync(string appId, int version = 0, CancellationToken cancellationToken = default) =>
        snapshotService.GetResolvedPublishedSnapshotAsync(appId, version, cancellationToken);

    /// <summary>
    /// 按版本列出发布快照配置项（保留分组信息，供管理端树形展示）。
    /// </summary>
    public async Task<IReadOnlyList<ConfigReleaseItemDto>> ListReleaseItemsByVersionAsync(
        string appId,
        int version,
        CancellationToken cancellationToken = default)
    {
        await applicationService.RequireApplicationAsync(appId, cancellationToken);
        var normalizedAppId = appId.Trim();
        var releases = await repository.ListReleasesAsync(normalizedAppId, cancellationToken);
        var release = releases.FirstOrDefault(item => item.Version == version)
                      ?? throw new NotFoundDomainException($"Release v{version} not found.");

        var items = await repository.ListReleaseItemsAsync(release.Id, cancellationToken);
        return items
            .Select(item => new ConfigReleaseItemDto(item.Group, item.Key, item.Value, item.ValueType))
            .ToList();
    }

    /// <summary>
    /// 获取管理端已发布快照（扁平字典 + 分组配置项）。
    /// </summary>
    public async Task<AdminPublishedConfigSnapshot> GetAdminPublishedSnapshotRequiredAsync(
        string appId,
        int version = 0,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await GetPublishedSnapshotRequiredAsync(appId, version, cancellationToken);
        var items = await ListReleaseItemsByVersionAsync(appId, snapshot.Version, cancellationToken);
        return new AdminPublishedConfigSnapshot(snapshot.Version, snapshot.Data, items);
    }

    /// <summary>
    /// 按发布主键获取配置快照。
    /// </summary>
    public Task<PublishedConfigSnapshot> GetPublishedSnapshotByReleaseIdAsync(long releaseId, CancellationToken cancellationToken = default) =>
        snapshotService.GetPublishedSnapshotByReleaseIdAsync(releaseId, cancellationToken);

    /// <summary>
    /// 按发布主键获取已解析 Secret 的配置快照。
    /// </summary>
    public Task<PublishedConfigSnapshot> GetResolvedPublishedSnapshotByReleaseIdAsync(long releaseId, CancellationToken cancellationToken = default) =>
        snapshotService.GetResolvedPublishedSnapshotByReleaseIdAsync(releaseId, cancellationToken);
}
