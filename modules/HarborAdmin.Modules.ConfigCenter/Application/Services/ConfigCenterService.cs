using HarborAdmin.BuildingBlocks.Abstractions.Secrets;
using HarborAdmin.BuildingBlocks.Data;
using HarborAdmin.BuildingBlocks.Mapping;
using HarborAdmin.BuildingBlocks.Secrets.References;
using HarborAdmin.Modules.ConfigCenter.Contracts.Dtos;
using HarborAdmin.Modules.ConfigCenter.Contracts.Requests;
using HarborAdmin.Modules.ConfigCenter.Application.Abstractions;
using HarborAdmin.Modules.ConfigCenter.Domain.Entities;
using HarborAdmin.Modules.ConfigCenter.Infrastructure.Contexts;
using System.Text.Json;

namespace HarborAdmin.Modules.ConfigCenter.Application.Services;

/// <summary>
/// 配置中心应用服务:应用/草稿 CRUD,发布,已发布快照查询
/// </summary>
public sealed class ConfigCenterService(
    IConfigCenterRepository repository,
    IConfigCenterDbContext dbContext,
    DbEntityRegistry entityRegistry,
    UnitOfWorkManagerCloud unitOfWorkManager,
    IConfigCenterNotifyClient notifyClient,
    ISecretStore secretStore,
    ConfigCenterSnapshotService snapshotService,
    IHarborMapper mapper)
{
    /// <summary>
    /// 列出所有应用
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>应用列表</returns>
    public async Task<IReadOnlyList<ConfigApplicationDto>> ListApplicationsAsync(CancellationToken cancellationToken = default) =>
        (await repository.ListApplicationsAsync(cancellationToken))
        .Select(application => mapper.Map<ConfigApplicationDto>(application))
        .ToList();

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
            CreatedAt = UtcTimestamp()
        };

        var created = await repository.InsertApplicationAsync(entity, cancellationToken);
        return mapper.Map<ConfigApplicationDto>(created);
    }

    /// <summary>
    /// 更新应用元数据
    /// </summary>
    /// <exception cref="KeyNotFoundException">应用不存在</exception>
    public async Task<ConfigApplicationDto> UpdateApplicationAsync(string appId, UpdateConfigApplicationRequest request,
        CancellationToken cancellationToken = default)
    {
        var entity = await RequireApplicationAsync(appId, cancellationToken);
        entity.Name = request.Name.Trim();
        entity.Description = request.Description?.Trim();
        await repository.UpdateApplicationAsync(entity, cancellationToken);
        return mapper.Map<ConfigApplicationDto>(entity);
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
    public async Task<IReadOnlyList<ConfigItemDto>> ListItemsAsync(string appId, CancellationToken cancellationToken = default)
    {
        await RequireApplicationAsync(appId, cancellationToken);
        var items = await repository.ListItemsAsync(appId.Trim(), cancellationToken);
        return items.Select(item => mapper.Map<ConfigItemDto>(item)).ToList();
    }

    /// <summary>
    /// 新增草稿配置项
    /// </summary>
    /// <exception cref="KeyNotFoundException">应用不存在</exception>
    /// <exception cref="ArgumentException">键或值无效</exception>
    public async Task<ConfigItemDto> CreateItemAsync(string appId, CreateConfigItemRequest request,
        CancellationToken cancellationToken = default)
    {
        await RequireApplicationAsync(appId, cancellationToken);
        var valueType = NormalizeValueType(request.ValueType);
        var value = await NormalizeItemValueAsync(request.Value, valueType, cancellationToken);
        ValidateItemRequest(request.Key, value, valueType);

        var entity = new ConfigItem
        {
            AppId = appId.Trim(),
            Group = request.Group.Trim(),
            Key = request.Key.Trim(),
            Value = value,
            ValueType = valueType,
            Remark = request.Remark?.Trim(),
            UpdatedAt = UtcTimestamp()
        };

        var created = await repository.InsertItemAsync(entity, cancellationToken);
        return mapper.Map<ConfigItemDto>(created);
    }

    /// <summary>
    /// 更新草稿配置项
    /// </summary>
    /// <exception cref="KeyNotFoundException">配置项不存在</exception>
    public async Task<ConfigItemDto> UpdateItemAsync(long id, UpdateConfigItemRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await repository.GetItemAsync(id, cancellationToken) ?? throw new KeyNotFoundException($"Config item {id} not found.");

        var valueType = NormalizeValueType(request.ValueType);
        var value = await NormalizeItemValueAsync(request.Value, valueType, cancellationToken);
        ValidateItemRequest(request.Key, value, valueType);
        entity.Group = request.Group.Trim();
        entity.Key = request.Key.Trim();
        entity.Value = value;
        entity.ValueType = valueType;
        entity.Remark = request.Remark?.Trim();
        entity.UpdatedAt = UtcTimestamp();

        await repository.UpdateItemAsync(entity, cancellationToken);
        return mapper.Map<ConfigItemDto>(entity);
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
    public async Task<IReadOnlyList<ConfigReleaseDto>> ListReleasesAsync(string appId, CancellationToken cancellationToken = default)
    {
        await RequireApplicationAsync(appId, cancellationToken);
        var releases = await repository.ListReleasesAsync(appId.Trim(), cancellationToken);
        return releases.Select(release => mapper.Map<ConfigReleaseDto>(release)).ToList();
    }

    /// <summary>
    /// 将当前草稿快照为新的发布版本,并通过 TCP 通知 ConfigCenter 进程刷新与广播
    /// </summary>
    /// <exception cref="KeyNotFoundException">应用不存在</exception>
    public async Task<PublishConfigResult> PublishAsync(string appId, PublishConfigRequest request,
        CancellationToken cancellationToken = default)
    {
        await RequireApplicationAsync(appId, cancellationToken);
        var normalizedAppId = appId.Trim();

        var draftItems = await repository.ListItemsAsync(normalizedAppId, cancellationToken);
        var latest = await repository.GetLatestReleaseAsync(normalizedAppId, cancellationToken);
        var nextVersion = (latest?.Version ?? 0) + 1;

        var release = new ConfigRelease
        {
            AppId = normalizedAppId,
            Version = nextVersion,
            PublishedBy = request.PublishedBy?.Trim(),
            PublishedAt = UtcTimestamp()
        };

        var releaseItems = new List<ConfigReleaseItem>(draftItems.Count);
        foreach (var item in draftItems)
        {
            releaseItems.Add(new ConfigReleaseItem
            {
                Group = item.Group,
                Key = item.Key,
                Value = await PinSecretReferencesAsync(item.Value, item.ValueType, cancellationToken),
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
    /// 获取已发布配置快照:<paramref name="version"/> 为 0 时取最新版本
    /// </summary>
    /// <param name="appId">AppId</param>
    /// <param name="version">版本号</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>不存在时返回 null</returns>
    public async Task<PublishedConfigSnapshot?> GetPublishedSnapshotAsync(string appId, int version = 0,
        CancellationToken cancellationToken = default) =>
        await snapshotService.GetPublishedSnapshotAsync(appId, version, cancellationToken);

    /// <summary>
    /// 获取已发布配置快照并在内存中解析 Secret 引用，供 ConfigCenter TCP 下发使用。
    /// </summary>
    public async Task<PublishedConfigSnapshot?> GetResolvedPublishedSnapshotAsync(string appId, int version = 0,
        CancellationToken cancellationToken = default) =>
        await snapshotService.GetResolvedPublishedSnapshotAsync(appId, version, cancellationToken);

    /// <summary>
    /// 按发布主键获取配置快照
    /// </summary>
    /// <exception cref="KeyNotFoundException">发布记录不存在</exception>
    public async Task<PublishedConfigSnapshot> GetPublishedSnapshotByReleaseIdAsync(long releaseId, CancellationToken cancellationToken = default) =>
        await snapshotService.GetPublishedSnapshotByReleaseIdAsync(releaseId, cancellationToken);

    /// <summary>
    /// 按发布主键获取配置快照并在内存中解析 Secret 引用，供 ConfigCenter TCP 下发使用。
    /// </summary>
    public async Task<PublishedConfigSnapshot> GetResolvedPublishedSnapshotByReleaseIdAsync(long releaseId,
        CancellationToken cancellationToken = default) =>
        await snapshotService.GetResolvedPublishedSnapshotByReleaseIdAsync(releaseId, cancellationToken);

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
    /// <param name="valueType">值类型</param>
    private static void ValidateItemRequest(string key, string value, string valueType)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Key is required.");
        }

        if (value is null)
        {
            throw new ArgumentException("Value is required.");
        }

        if (IsStructuredValueType(valueType))
        {
            try
            {
                using var _ = JsonDocument.Parse(value);
            }
            catch (JsonException ex)
            {
                throw new ArgumentException("Value must be valid JSON when ValueType is json/object/options/model.", ex);
            }
        }
    }

    private static bool IsStructuredValueType(string valueType) =>
        valueType.Trim().ToLowerInvariant() is "json" or "object" or "options" or "model";

    private static bool IsSecretValueType(string valueType) =>
        valueType.Trim().Equals("secret", StringComparison.OrdinalIgnoreCase);

    private static string NormalizeValueType(string valueType) =>
        string.IsNullOrWhiteSpace(valueType) ? "string" : valueType.Trim().ToLowerInvariant();

    private async Task<string> NormalizeItemValueAsync(string value, string valueType, CancellationToken cancellationToken)
    {
        if (value is null)
        {
            throw new ArgumentException("Value is required.");
        }

        if (IsSecretValueType(valueType))
        {
            return await NormalizeSecretMarkerAsync(value, cancellationToken);
        }

        await ValidateSecretReferencesAsync(value, cancellationToken);
        return value;
    }

    private async Task<string> NormalizeSecretMarkerAsync(string value, CancellationToken cancellationToken)
    {
        var normalized = value.Trim();
        if (SecretReferenceParser.TryParseSingle(normalized, out var reference))
        {
            await RequireSecretReferenceAsync(reference, cancellationToken);
            return SecretReferenceParser.Format(reference.SecretRef, reference.Version);
        }

        if (!SecretReferenceParser.IsValidRef(normalized))
        {
            throw new ArgumentException("ValueType secret requires a SecretRef or ${secret:ref} marker.");
        }

        var descriptor = await secretStore.GetAsync(normalized, cancellationToken);
        if (descriptor is not { Enabled: true })
        {
            throw new ArgumentException($"SecretRef '{normalized}' does not exist or is disabled.");
        }

        return SecretReferenceParser.Format(descriptor.SecretRef);
    }

    private async Task ValidateSecretReferencesAsync(string value, CancellationToken cancellationToken)
    {
        foreach (var reference in SecretReferenceParser.Find(value))
        {
            await RequireSecretReferenceAsync(reference, cancellationToken);
        }
    }

    private async Task RequireSecretReferenceAsync(SecretReferenceToken reference, CancellationToken cancellationToken)
    {
        var descriptor = await secretStore.GetAsync(reference.SecretRef, cancellationToken);
        if (descriptor is not { Enabled: true })
        {
            throw new ArgumentException($"SecretRef '{reference.SecretRef}' does not exist or is disabled.");
        }

        if (reference.Version is { } version &&
            await secretStore.GetVersionAsync(reference.SecretRef, version, cancellationToken) is null)
        {
            throw new ArgumentException($"SecretRef '{reference.SecretRef}' version {version} does not exist.");
        }
    }

    private async Task<string> PinSecretReferencesAsync(string value, string valueType, CancellationToken cancellationToken)
    {
        var normalized = IsSecretValueType(valueType) && !SecretReferenceParser.TryParseSingle(value.Trim(), out _)
            ? await NormalizeSecretMarkerAsync(value, cancellationToken)
            : value;
        if (!SecretReferenceParser.Contains(normalized))
        {
            return normalized;
        }

        return await SecretReferenceParser.ReplaceAsync(normalized, async (reference, token) =>
        {
            await RequireSecretReferenceAsync(reference, token);
            if (reference.Version is { } version)
            {
                return SecretReferenceParser.Format(reference.SecretRef, version);
            }

            var descriptor = await secretStore.GetAsync(reference.SecretRef, token)
                             ?? throw new ArgumentException($"SecretRef '{reference.SecretRef}' does not exist.");
            return SecretReferenceParser.Format(reference.SecretRef, descriptor.Version);
        }, cancellationToken);
    }

    private static DateTimeOffset UtcTimestamp() =>
        DateTimeOffset.UtcNow;

}
