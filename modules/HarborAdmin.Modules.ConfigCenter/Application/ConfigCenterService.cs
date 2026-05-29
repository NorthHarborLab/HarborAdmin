using HarborAdmin.Modules.ConfigCenter.Contracts;
using HarborAdmin.Modules.ConfigCenter.Domain;

namespace HarborAdmin.Modules.ConfigCenter.Application;

/// <summary>
/// 配置中心应用服务:应用/草稿 CRUD,发布,已发布快照查询
/// </summary>
/// <param name="repository">持久化仓储</param>
/// <param name="notifyClient">发布后通知 ConfigCenter 进程的客户端</param>
public sealed class ConfigCenterService(IConfigCenterRepository repository, IConfigCenterNotifyClient notifyClient)
{
    /// <summary>
    /// 列出所有应用
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>应用列表</returns>
    public Task<IReadOnlyList<ConfigApplicationDto>> ListApplicationsAsync(CancellationToken cancellationToken = default) =>
        repository.ListApplicationsAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<ConfigApplicationDto>)t.Result.Select(ToDto).ToList(), cancellationToken);

    /// <summary>
    /// 注册新应用
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="request"/>.AppId 为空。</exception>
    /// <exception cref="InvalidOperationException">AppId 已存在。</exception>
    public async Task<ConfigApplicationDto> CreateApplicationAsync(CreateConfigApplicationRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.AppId))
        {
            throw new ArgumentException("AppId is required.", nameof(request));
        }

        var existing = await repository.GetApplicationByAppIdAsync(request.AppId.Trim(), cancellationToken);
        if (existing is not null)
        {
            throw new InvalidOperationException($"Application '{request.AppId}' already exists.");
        }

        var entity = new ConfigApplication
        {
            AppId = request.AppId.Trim(),
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        var created = await repository.InsertApplicationAsync(entity, cancellationToken);
        return ToDto(created);
    }

    /// <summary>
    /// 更新应用元数据
    /// </summary>
    /// <exception cref="KeyNotFoundException">应用不存在</exception>
    public async Task<ConfigApplicationDto> UpdateApplicationAsync(string appId, UpdateConfigApplicationRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await RequireApplicationAsync(appId, cancellationToken);
        entity.Name = request.Name.Trim();
        entity.Description = request.Description?.Trim();
        await repository.UpdateApplicationAsync(entity, cancellationToken);
        return ToDto(entity);
    }

    /// <summary>
    /// 删除应用及其全部配置数据
    /// </summary>
    /// <exception cref="KeyNotFoundException">应用不存在</exception>
    public async Task DeleteApplicationAsync(string appId, CancellationToken cancellationToken = default)
    {
        _ = await RequireApplicationAsync(appId, cancellationToken);
        await repository.DeleteApplicationAsync(appId.Trim(), cancellationToken);
    }

    /// <summary>
    /// 列出草稿配置项
    /// </summary>
    /// <exception cref="KeyNotFoundException">应用不存在</exception>
    public async Task<IReadOnlyList<ConfigItemDto>> ListItemsAsync(string appId, string environment, CancellationToken cancellationToken = default)
    {
        await RequireApplicationAsync(appId, cancellationToken);
        var items = await repository.ListItemsAsync(appId.Trim(), environment.Trim(), cancellationToken);
        return items.Select(ToDto).ToList();
    }

    /// <summary>
    /// 新增草稿配置项
    /// </summary>
    /// <exception cref="KeyNotFoundException">应用不存在</exception>
    /// <exception cref="ArgumentException">键或值无效</exception>
    public async Task<ConfigItemDto> CreateItemAsync(string appId, string environment, CreateConfigItemRequest request, CancellationToken cancellationToken = default)
    {
        await RequireApplicationAsync(appId, cancellationToken);
        ValidateItemRequest(request.Key, request.Value);

        var entity = new ConfigItem
        {
            AppId = appId.Trim(),
            Environment = environment.Trim(),
            Group = request.Group.Trim(),
            Key = request.Key.Trim(),
            Value = request.Value,
            ValueType = string.IsNullOrWhiteSpace(request.ValueType) ? "string" : request.ValueType.Trim(),
            Remark = request.Remark?.Trim(),
            UpdatedAt = DateTime.UtcNow
        };

        var created = await repository.InsertItemAsync(entity, cancellationToken);
        return ToDto(created);
    }

    /// <summary>
    /// 更新草稿配置项
    /// </summary>
    /// <exception cref="KeyNotFoundException">配置项不存在</exception>
    public async Task<ConfigItemDto> UpdateItemAsync(long id, UpdateConfigItemRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await repository.GetItemAsync(id, cancellationToken) ?? throw new KeyNotFoundException($"Config item {id} not found.");

        ValidateItemRequest(request.Key, request.Value);
        entity.Group = request.Group.Trim();
        entity.Key = request.Key.Trim();
        entity.Value = request.Value;
        entity.ValueType = string.IsNullOrWhiteSpace(request.ValueType) ? "string" : request.ValueType.Trim();
        entity.Remark = request.Remark?.Trim();
        entity.UpdatedAt = DateTime.UtcNow;

        await repository.UpdateItemAsync(entity, cancellationToken);
        return ToDto(entity);
    }

    /// <summary>
    /// 删除草稿配置项
    /// </summary>
    /// <exception cref="KeyNotFoundException">配置项不存在</exception>
    public async Task DeleteItemAsync(long id, CancellationToken cancellationToken = default)
    {
        _ = await repository.GetItemAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Config item {id} not found.");
        await repository.DeleteItemAsync(id, cancellationToken);
    }

    /// <summary>
    /// 列出发布历史
    /// </summary>
    /// <exception cref="KeyNotFoundException">应用不存在</exception>
    public async Task<IReadOnlyList<ConfigReleaseDto>> ListReleasesAsync(string appId, string environment, CancellationToken cancellationToken = default)
    {
        await RequireApplicationAsync(appId, cancellationToken);
        var releases = await repository.ListReleasesAsync(appId.Trim(), environment.Trim(), cancellationToken);
        return releases.Select(ToDto).ToList();
    }

    /// <summary>
    /// 将当前草稿快照为新的发布版本,并通知 ConfigCenter 进程刷新与广播
    /// </summary>
    /// <exception cref="KeyNotFoundException">应用不存在</exception>
    public async Task<PublishConfigResult> PublishAsync(string appId, string environment, PublishConfigRequest request, CancellationToken cancellationToken = default)
    {
        await RequireApplicationAsync(appId, cancellationToken);
        var normalizedAppId = appId.Trim();
        var normalizedEnv = environment.Trim();

        var draftItems = await repository.ListItemsAsync(normalizedAppId, normalizedEnv, cancellationToken);
        var latest = await repository.GetLatestReleaseAsync(normalizedAppId, normalizedEnv, cancellationToken);
        var nextVersion = (latest?.Version ?? 0) + 1;

        var release = new ConfigRelease
        {
            AppId = normalizedAppId,
            Environment = normalizedEnv,
            Version = nextVersion,
            PublishedBy = request.PublishedBy?.Trim(),
            PublishedAt = DateTime.UtcNow
        };

        var releaseItems = draftItems.Select(item => new ConfigReleaseItem
        {
            Group = item.Group,
            Key = item.Key,
            Value = item.Value,
            ValueType = item.ValueType
        }).ToList();

        var created = await repository.InsertReleaseAsync(release, releaseItems, cancellationToken);
        await notifyClient.NotifyPublishedAsync(normalizedAppId, normalizedEnv, created.Id, cancellationToken);

        return new PublishConfigResult(created.Id, created.Version);
    }

    /// <summary>
    /// 获取已发布配置快照:<paramref name="version"/> 为 0 时取最新版本
    /// </summary>
    /// <param name="appId">AppId</param>
    /// <param name="environment">环境</param>
    /// <param name="version">版本号</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>不存在时返回 null</returns>
    public async Task<PublishedConfigSnapshot?> GetPublishedSnapshotAsync(string appId, string environment, int version = 0, CancellationToken cancellationToken = default)
    {
        var normalizedAppId = appId.Trim();
        var normalizedEnv = environment.Trim();

        ConfigRelease? release;
        if (version > 0)
        {
            var releases = await repository.ListReleasesAsync(normalizedAppId, normalizedEnv, cancellationToken);
            release = releases.FirstOrDefault(r => r.Version == version);
        }
        else
        {
            release = await repository.GetLatestReleaseAsync(normalizedAppId, normalizedEnv, cancellationToken);
        }

        if (release is null)
        {
            return null;
        }

        var items = await repository.ListReleaseItemsAsync(release.Id, cancellationToken);
        var data = items.ToDictionary(i => i.ConfigKey, i => i.Value, StringComparer.OrdinalIgnoreCase);
        return new PublishedConfigSnapshot(release.Version, data);
    }

    /// <summary>
    /// 按发布主键获取配置快照
    /// </summary>
    /// <exception cref="KeyNotFoundException">发布记录不存在</exception>
    public async Task<PublishedConfigSnapshot> GetPublishedSnapshotByReleaseIdAsync(long releaseId, CancellationToken cancellationToken = default)
    {
        var release = await repository.GetReleaseByIdAsync(releaseId, cancellationToken)
                      ?? throw new KeyNotFoundException($"Release {releaseId} not found.");

        var items = await repository.ListReleaseItemsAsync(release.Id, cancellationToken);
        var data = items.ToDictionary(i => i.ConfigKey, i => i.Value, StringComparer.OrdinalIgnoreCase);
        return new PublishedConfigSnapshot(release.Version, data);
    }

    /// <summary>
    /// 确保应用存在,否则抛出 <see cref="KeyNotFoundException"/>
    /// </summary>
    private async Task<ConfigApplication> RequireApplicationAsync(string appId, CancellationToken cancellationToken)
    {
        return await repository.GetApplicationByAppIdAsync(appId.Trim(), cancellationToken)
               ?? throw new KeyNotFoundException($"Application '{appId}' not found.");
    }

    /// <summary>
    /// 校验配置项必填字段
    /// </summary>
    /// <param name="key">键</param>
    /// <param name="value">值</param>
    private static void ValidateItemRequest( string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Key is required.");
        }

        if (value is null)
        {
            throw new ArgumentException("Value is required.");
        }
    }

    /// <summary>
    /// 领域实体转应用 DTO
    /// </summary>
    private static ConfigApplicationDto ToDto(ConfigApplication entity) =>
        new(entity.Id, entity.AppId, entity.Name, entity.Description, entity.CreatedAt);

    /// <summary>
    /// 领域实体转配置项 DTO
    /// </summary>
    private static ConfigItemDto ToDto(ConfigItem entity) =>
        new(entity.Id, entity.AppId, entity.Environment, entity.Group, entity.Key, entity.Value, entity.ValueType,
            entity.Remark, entity.UpdatedAt);

    /// <summary>
    /// 领域实体转发布记录 DTO
    /// </summary>
    private static ConfigReleaseDto ToDto(ConfigRelease entity) =>
        new(entity.Id, entity.AppId, entity.Environment, entity.Version, entity.PublishedBy, entity.PublishedAt);
}