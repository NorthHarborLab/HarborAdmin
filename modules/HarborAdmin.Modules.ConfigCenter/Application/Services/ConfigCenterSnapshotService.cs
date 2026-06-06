using System.Text.Json;
using HarborAdmin.BuildingBlocks.Abstractions.Api;
using HarborAdmin.BuildingBlocks.Abstractions.Exception;
using HarborAdmin.BuildingBlocks.Abstractions.Secrets;
using HarborAdmin.BuildingBlocks.Secrets.References;
using HarborAdmin.Modules.ConfigCenter.Application.Abstractions;
using HarborAdmin.Modules.ConfigCenter.Contracts.Dtos;
using HarborAdmin.Modules.ConfigCenter.Domain.Entities;
using Microsoft.AspNetCore.Http;

namespace HarborAdmin.Modules.ConfigCenter.Application.Services;

/// <summary>
/// 配置中心已发布快照读取服务。
/// </summary>
public sealed class ConfigCenterSnapshotService(
    IConfigCenterRepository repository,
    ISecretStore secretStore)
{
    /// <summary>
    /// 获取已发布配置快照。
    /// </summary>
    public async Task<PublishedConfigSnapshot?> GetPublishedSnapshotAsync(
        string appId,
        int version = 0,
        CancellationToken cancellationToken = default)
    {
        var normalizedAppId = appId.Trim();

        ConfigRelease? release;
        if (version > 0)
        {
            var releases = await repository.ListReleasesAsync(normalizedAppId, cancellationToken);
            release = releases.FirstOrDefault(r => r.Version == version);
        }
        else
        {
            release = await repository.GetLatestReleaseAsync(normalizedAppId, cancellationToken);
        }

        if (release is null)
        {
            return null;
        }

        var items = await repository.ListReleaseItemsAsync(release.Id, cancellationToken);
        var data = BuildSnapshotData(items);
        return new PublishedConfigSnapshot(release.Version, data);
    }

    /// <summary>
    /// 获取已发布配置快照，并在内存中解析 Secret 引用。
    /// </summary>
    public async Task<PublishedConfigSnapshot?> GetResolvedPublishedSnapshotAsync(
        string appId,
        int version = 0,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await GetPublishedSnapshotAsync(appId, version, cancellationToken);
        return snapshot is null
            ? null
            : new PublishedConfigSnapshot(snapshot.Version, await ResolveSnapshotDataAsync(snapshot.Data, cancellationToken));
    }

    /// <summary>
    /// 按发布主键获取配置快照。
    /// </summary>
    public async Task<PublishedConfigSnapshot> GetPublishedSnapshotByReleaseIdAsync(
        long releaseId,
        CancellationToken cancellationToken = default)
    {
        var release = await repository.GetReleaseByIdAsync(releaseId, cancellationToken)
                      ?? throw new NotFoundDomainException($"Release {releaseId} not found.");

        var items = await repository.ListReleaseItemsAsync(release.Id, cancellationToken);
        var data = BuildSnapshotData(items);
        return new PublishedConfigSnapshot(release.Version, data);
    }

    /// <summary>
    /// 按发布主键获取配置快照，并在内存中解析 Secret 引用。
    /// </summary>
    public async Task<PublishedConfigSnapshot> GetResolvedPublishedSnapshotByReleaseIdAsync(
        long releaseId,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await GetPublishedSnapshotByReleaseIdAsync(releaseId, cancellationToken);
        return new PublishedConfigSnapshot(snapshot.Version, await ResolveSnapshotDataAsync(snapshot.Data, cancellationToken));
    }

    private static IReadOnlyDictionary<string, string> BuildSnapshotData(IEnumerable<ConfigReleaseItem> items)
    {
        var data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in items)
        {
            var configKey = item.ConfigKey;
            if (IsStructuredValueType(item.ValueType))
            {
                AddStructuredValue(data, configKey, item.Value);
                continue;
            }

            data[configKey] = item.Value;
        }

        return data;
    }

    private async Task<IReadOnlyDictionary<string, string>> ResolveSnapshotDataAsync(
        IReadOnlyDictionary<string, string> data,
        CancellationToken cancellationToken)
    {
        var resolved = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in data)
        {
            resolved[key] = SecretReferenceParser.Contains(value)
                ? await ResolveSecretReferencesAsync(value, key, cancellationToken)
                : value;
        }

        return resolved;
    }

    private async Task<string> ResolveSecretReferencesAsync(
        string value,
        string configKey,
        CancellationToken cancellationToken) =>
        await SecretReferenceParser.ReplaceAsync(value, async (reference, token) =>
        {
            var secret = await secretStore.ResolveAsync(reference.SecretRef, reference.Version, token);
            if (secret is null)
            {
                throw new BusinessDomainException(
                    ApiResultCodes.InternalError,
                    $"Secret reference for config key '{configKey}' cannot be resolved.",
                    StatusCodes.Status500InternalServerError);
            }

            return secret;
        }, cancellationToken);

    private static bool IsStructuredValueType(string valueType) =>
        valueType.Trim().ToLowerInvariant() is "json" or "object" or "options" or "model";

    private static void AddStructuredValue(IDictionary<string, string> data, string baseKey, string value)
    {
        using var document = JsonDocument.Parse(value);
        AddJsonElement(data, baseKey, document.RootElement);
    }

    private static void AddJsonElement(IDictionary<string, string> data, string key, JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    AddJsonElement(data, $"{key}:{property.Name}", property.Value);
                }

                break;
            case JsonValueKind.Array:
                var index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    AddJsonElement(data, $"{key}:{index}", item);
                    index++;
                }

                break;
            case JsonValueKind.String:
                data[key] = element.GetString() ?? string.Empty;
                break;
            case JsonValueKind.Number:
            case JsonValueKind.True:
            case JsonValueKind.False:
                data[key] = element.GetRawText();
                break;
            case JsonValueKind.Null:
            case JsonValueKind.Undefined:
                data[key] = string.Empty;
                break;
        }
    }
}
