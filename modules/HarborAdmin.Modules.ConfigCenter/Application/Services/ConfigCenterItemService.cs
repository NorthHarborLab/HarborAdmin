using System.Text.Json;
using HarborAdmin.BuildingBlocks.Abstractions.Exception;
using HarborAdmin.BuildingBlocks.Mapping;
using HarborAdmin.Modules.ConfigCenter.Application.Abstractions;
using HarborAdmin.Modules.ConfigCenter.Contracts.Dtos;
using HarborAdmin.Modules.ConfigCenter.Contracts.Requests;
using HarborAdmin.Modules.ConfigCenter.Domain.Entities;

namespace HarborAdmin.Modules.ConfigCenter.Application.Services;

/// <summary>
/// 配置中心草稿项管理服务。
/// </summary>
public sealed class ConfigCenterItemService(
    IConfigCenterRepository repository,
    ConfigCenterApplicationService applicationService,
    ConfigSecretReferenceValidator secretValidator,
    IHarborMapper mapper)
{
    /// <summary>
    /// 列出草稿配置项。
    /// </summary>
    public async Task<IReadOnlyList<ConfigItemDto>> ListItemsAsync(string appId, CancellationToken cancellationToken = default)
    {
        await applicationService.RequireApplicationAsync(appId, cancellationToken);
        var items = await repository.ListItemsAsync(appId.Trim(), cancellationToken);
        return items.Select(item => mapper.Map<ConfigItemDto>(item)).ToList();
    }

    /// <summary>
    /// 新增草稿配置项。
    /// </summary>
    public async Task<ConfigItemDto> CreateItemAsync(string appId, CreateConfigItemRequest request,
        CancellationToken cancellationToken = default)
    {
        await applicationService.RequireApplicationAsync(appId, cancellationToken);
        var valueType = NormalizeValueType(request.ValueType);
        var value = await NormalizeItemValueAsync(request.Value, valueType, cancellationToken);
        ValidateStructuredValue(request.Value, valueType);

        var entity = new ConfigItem
        {
            AppId = appId.Trim(),
            Group = request.Group.Trim(),
            Key = request.Key.Trim(),
            Value = value,
            ValueType = valueType,
            Remark = request.Remark?.Trim(),
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var created = await repository.InsertItemAsync(entity, cancellationToken);
        return mapper.Map<ConfigItemDto>(created);
    }

    /// <summary>
    /// 更新草稿配置项。
    /// </summary>
    public async Task<ConfigItemDto> UpdateItemAsync(long id, UpdateConfigItemRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await repository.GetItemAsync(id, cancellationToken)
                     ?? throw new NotFoundDomainException($"配置项 {id} 不存在。");

        var valueType = NormalizeValueType(request.ValueType);
        var value = await NormalizeItemValueAsync(request.Value, valueType, cancellationToken);
        ValidateStructuredValue(request.Value, valueType);
        entity.Group = request.Group.Trim();
        entity.Key = request.Key.Trim();
        entity.Value = value;
        entity.ValueType = valueType;
        entity.Remark = request.Remark?.Trim();
        entity.UpdatedAt = DateTimeOffset.UtcNow;

        await repository.UpdateItemAsync(entity, cancellationToken);
        return mapper.Map<ConfigItemDto>(entity);
    }

    /// <summary>
    /// 删除草稿配置项。
    /// </summary>
    public async Task DeleteItemAsync(long id, CancellationToken cancellationToken = default)
    {
        _ = await repository.GetItemAsync(id, cancellationToken)
            ?? throw new NotFoundDomainException($"配置项 {id} 不存在。");
        await repository.DeleteItemAsync(id, cancellationToken);
    }

    private static string NormalizeValueType(string valueType) =>
        string.IsNullOrWhiteSpace(valueType) ? "string" : valueType.Trim().ToLowerInvariant();

    private static bool IsStructuredValueType(string valueType) =>
        valueType.Trim().ToLowerInvariant() is "json" or "object" or "options" or "model";

    private static bool IsSecretValueType(string valueType) =>
        valueType.Trim().Equals("secret", StringComparison.OrdinalIgnoreCase);

    private static void ValidateStructuredValue(string value, string valueType)
    {
        if (!IsStructuredValueType(valueType))
        {
            return;
        }

        try
        {
            using var _ = JsonDocument.Parse(value);
        }
        catch (JsonException ex)
        {
            throw new ValidationDomainException("结构化类型配置值必须是合法 JSON。", innerException: ex);
        }
    }

    private async Task<string> NormalizeItemValueAsync(string value, string valueType, CancellationToken cancellationToken)
    {
        if (IsSecretValueType(valueType))
        {
            return await secretValidator.NormalizeSecretMarkerAsync(value, cancellationToken);
        }

        await secretValidator.ValidateSecretReferencesAsync(value, cancellationToken);
        return value;
    }
}
