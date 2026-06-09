using HarborAdmin.BuildingBlocks.Abstractions.Exception;
using HarborAdmin.Modules.Admin.Application.Services.Access;
using HarborAdmin.Modules.Admin.Application.Services.Dictionary;
using HarborAdmin.Modules.Admin.Contracts.DynamicCrud.Dto;
using HarborAdmin.Modules.Admin.Infrastructure.Caching;

namespace HarborAdmin.Modules.Admin.Application.Services.Metadata;

/// <summary>
/// Admin 动态 Feature schema 服务。
/// </summary>
public sealed class AdminMetadataService(AccessCacheService accessCache, AdminFieldOptionResolver optionResolver)
{
    /// <summary>
    /// 获取指定动态 Feature schema。
    /// </summary>
    public async Task<DynamicViewSchemaDto> GetSchemaAsync(string featureCode, AdminFieldPermissionSet accessSet, CancellationToken cancellationToken)
    {
        var normalized = NormalizeFeatureCode(featureCode);
        var schema = await accessCache.GetFeatureRuntimeSchemaAsync(normalized, cancellationToken);
        if (schema.FeatureType != Contracts.FeatureDesign.AdminFeatureType.Dynamic)
        {
            throw new ValidationDomainException($"Feature '{normalized}' is not dynamic.");
        }

        var fields = schema.Fields
            .Where(field => field.Enabled)
            .OrderBy(field => field.SortOrder)
            .Where(field => accessSet.IsSuperAdmin || accessSet.VisibleFields.Contains(field.FieldCode))
            .ToArray();
        var actions = schema.Actions
            .Where(action => action.Enabled)
            .OrderBy(action => action.SortOrder)
            .ToArray();
        var apis = schema.Apis.ToArray();

        return new DynamicViewSchemaDto(
            schema.FeatureCode,
            schema.Name,
            schema.FeatureType,
            schema.Component ?? string.Empty,
            schema.RoutePath,
            schema.SchemaVersion,
            await ToFieldSchemasAsync(fields.Where(field => field.ListVisible), cancellationToken),
            await ToFieldSchemasAsync(fields.Where(field => field.SearchVisible), cancellationToken),
            await ToFieldSchemasAsync(fields.Where(field => field.CreateVisible || field.UpdateVisible), cancellationToken),
            actions.Select(ToActionSchema).ToList(),
            ToEndpointSchema(apis));
    }

    /// <summary>
    /// 批量转换字段运行时 schema。
    /// </summary>
    private async Task<IReadOnlyList<DynamicFieldSchemaDto>> ToFieldSchemasAsync(IEnumerable<FeatureFieldCacheItem> fields, CancellationToken cancellationToken)
    {
        var result = new List<DynamicFieldSchemaDto>();
        foreach (var field in fields)
        {
            result.Add(await ToFieldSchemaAsync(field, cancellationToken));
        }

        return result;
    }

    /// <summary>
    /// 转换字段运行时 schema。
    /// </summary>
    private async Task<DynamicFieldSchemaDto> ToFieldSchemaAsync(FeatureFieldCacheItem field, CancellationToken cancellationToken) =>
        new(
            field.FieldCode,
            field.LabelKey,
            field.LabelFallback,
            field.PlaceholderKey,
            field.PlaceholderFallback,
            field.Component,
            field.DataType,
            field.Required,
            field.Readonly,
            field.SortOrder,
            field.Width,
            field.DictCode,
            await optionResolver.ResolveDynamicOptionsAsync(field, cancellationToken),
            ParseValidation(field.ValidationJson));

    /// <summary>
    /// 转换动作运行时 schema。
    /// </summary>
    private static DynamicActionSchemaDto ToActionSchema(FeatureActionCacheItem action) =>
        new(
            action.ActionCode,
            action.LabelKey,
            action.LabelFallback,
            action.PermissionCode,
            action.SortOrder);

    /// <summary>
    /// 从 Feature API 列表中提取动态 CRUD 端点。
    /// </summary>
    private static DynamicEndpointSchemaDto? ToEndpointSchema(IReadOnlyList<FeatureApiCacheItem> apis)
    {
        if (apis.Count == 0)
        {
            return null;
        }

        var map = apis.ToDictionary(api => api.ApiCode, StringComparer.OrdinalIgnoreCase);
        return new DynamicEndpointSchemaDto(
            map.GetValueOrDefault("query")?.Path,
            map.GetValueOrDefault("detail")?.Path,
            map.GetValueOrDefault("create")?.Path,
            map.GetValueOrDefault("update")?.Path,
            map.GetValueOrDefault("delete")?.Path);
    }

    /// <summary>
    /// 解析字段校验 JSON。
    /// </summary>
    private static global::System.Text.Json.JsonElement? ParseValidation(string? validationJson)
    {
        if (string.IsNullOrWhiteSpace(validationJson))
        {
            return null;
        }

        return global::System.Text.Json.JsonSerializer.Deserialize<global::System.Text.Json.JsonElement>(
            validationJson,
            new global::System.Text.Json.JsonSerializerOptions(global::System.Text.Json.JsonSerializerDefaults.Web));
    }

    /// <summary>
    /// 规范化 Feature 编码。
    /// </summary>
    private static string NormalizeFeatureCode(string featureCode)
    {
        var normalized = featureCode.Trim();
        return string.IsNullOrWhiteSpace(normalized)
            ? throw new ValidationDomainException("功能编码不能为空。")
            : normalized;
    }
}