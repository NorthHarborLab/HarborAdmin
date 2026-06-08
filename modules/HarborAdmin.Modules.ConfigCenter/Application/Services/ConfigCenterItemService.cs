using System.Text.Json;
using HarborAdmin.BuildingBlocks.Mapping;
using HarborAdmin.Modules.ConfigCenter.Contracts.Item.Dto;
using HarborAdmin.Modules.ConfigCenter.Contracts.Item.Request;

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
        return items.Select(mapper.Map<ConfigItemDto>).ToList();
    }

    /// <summary>
    /// 保存草稿配置项（创建或更新）。
    /// </summary>
    public async Task<ConfigItemDto> SaveItemAsync(string appId, long? id, SaveConfigItemRequest request, CancellationToken cancellationToken = default)
    {
        if (id is null)
        {
            return await CreateItemAsync(appId, request, cancellationToken);
        }

        return await UpdateItemAsync(id.Value, request, cancellationToken);
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

    /// <summary>
    /// 创建草稿配置项并完成值类型规范化。
    /// </summary>
    private async Task<ConfigItemDto> CreateItemAsync(string appId, SaveConfigItemRequest request, CancellationToken cancellationToken)
    {
        await applicationService.RequireApplicationAsync(appId, cancellationToken);
        var valueType = NormalizeValueType(request.ValueType);
        var value = await NormalizeItemValueAsync(request.Value, valueType, cancellationToken);
        // 结构化类型发布时会被展开为扁平键，保存草稿前先保证 JSON 可解析。
        ValidateStructuredValue(request.Value, valueType);

        var entity = new ConfigItem
        {
            AppId = appId.Trim(),
            Group = request.Group.Trim(),
            Key = request.Key.Trim(),
            Value = value,
            ValueType = valueType,
            Remark = request.Remark?.Trim()
        };

        var created = await repository.InsertItemAsync(entity, cancellationToken);
        return mapper.Map<ConfigItemDto>(created);
    }

    /// <summary>
    /// 更新草稿配置项并重新校验值与 Secret 引用。
    /// </summary>
    private async Task<ConfigItemDto> UpdateItemAsync(long id, SaveConfigItemRequest request, CancellationToken cancellationToken)
    {
        var entity = await repository.GetItemAsync(id, cancellationToken)
                     ?? throw new NotFoundDomainException($"配置项 {id} 不存在。");

        var valueType = NormalizeValueType(request.ValueType);
        var value = await NormalizeItemValueAsync(request.Value, valueType, cancellationToken);
        // 校验原始请求值，避免 Secret 类型规范化后绕过结构化 JSON 检查。
        ValidateStructuredValue(request.Value, valueType);
        entity.Group = request.Group.Trim();
        entity.Key = request.Key.Trim();
        entity.Value = value;
        entity.ValueType = valueType;
        entity.Remark = request.Remark?.Trim();

        await repository.UpdateItemAsync(entity, cancellationToken);
        return mapper.Map<ConfigItemDto>(entity);
    }

    /// <summary>
    /// 规范化配置值类型。
    /// </summary>
    private static string NormalizeValueType(string valueType) =>
        string.IsNullOrWhiteSpace(valueType) ? "string" : valueType.Trim().ToLowerInvariant();

    /// <summary>
    /// 判断值类型是否需要按 JSON 结构展开。
    /// </summary>
    private static bool IsStructuredValueType(string valueType) =>
        valueType.Trim().ToLowerInvariant() is "json" or "object" or "options" or "model";

    /// <summary>
    /// 判断值类型是否为 Secret 引用。
    /// </summary>
    private static bool IsSecretValueType(string valueType) =>
        valueType.Trim().Equals("secret", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// 校验结构化配置值必须是合法 JSON。
    /// </summary>
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

    /// <summary>
    /// 规范化配置值并校验其中的 Secret 引用。
    /// </summary>
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