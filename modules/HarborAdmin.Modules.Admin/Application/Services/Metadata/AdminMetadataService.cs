using System.Text.Json;
using HarborAdmin.BuildingBlocks.Abstractions.Exception;
using HarborAdmin.Modules.Admin.Application.Abstractions;
using HarborAdmin.Modules.Admin.Contracts.Access.Dto;
using HarborAdmin.Modules.Admin.Contracts.DynamicCrud.Dtos;
using HarborAdmin.Modules.Admin.Domain.Entities;

namespace HarborAdmin.Modules.Admin.Application.Services.Metadata;

/// <summary>
/// Admin 动态 Feature schema 服务。
/// </summary>
public sealed class AdminMetadataService(IAdminRepository repository)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// 获取指定动态 Feature schema。
    /// </summary>
    public async Task<DynamicViewSchemaDto> GetSchemaAsync(
        string featureCode,
        IReadOnlyList<FieldPolicyDto> fieldPolicies,
        CancellationToken cancellationToken)
    {
        var normalized = NormalizeFeatureCode(featureCode);
        var feature = await repository.GetEnabledFeatureRuntimeAsync(normalized, cancellationToken)
                      ?? throw new NotFoundDomainException($"Feature '{normalized}' was not found.");
        if (!feature.FeatureType.Equals("Dynamic", StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationDomainException($"Feature '{normalized}' is not dynamic.");
        }

        var policies = fieldPolicies
            .Where(policy => policy.FeatureCode == normalized)
            .ToDictionary(policy => policy.FieldName, StringComparer.OrdinalIgnoreCase);
        var fields = feature.Fields
            .Where(field => field.Enabled)
            .OrderBy(field => field.SortOrder)
            .Where(field => !policies.TryGetValue(field.FieldCode, out var policy) || policy.Visible)
            .ToArray();
        var actions = feature.Actions
            .Where(action => action.Enabled)
            .OrderBy(action => action.SortOrder)
            .ToArray();
        var apis = feature.Apis.Where(api => api.Enabled).ToArray();

        return new DynamicViewSchemaDto(
            feature.FeatureCode,
            feature.NameKey,
            feature.NameFallback,
            feature.FeatureType,
            feature.Component,
            feature.RoutePath,
            feature.SchemaVersion,
            fields.Where(field => field.ListVisible).Select(ToFieldSchema).ToList(),
            fields.Where(field => field.SearchVisible).Select(ToFieldSchema).ToList(),
            fields.Where(field => field.CreateVisible || field.UpdateVisible).Select(ToFieldSchema).ToList(),
            actions.Select(ToActionSchema).ToList(),
            ToEndpointSchema(apis));
    }

    private static DynamicFieldSchemaDto ToFieldSchema(AdminFeatureField field) =>
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
            ParseOptions(field.OptionsJson),
            ParseValidation(field.ValidationJson));

    private static DynamicActionSchemaDto ToActionSchema(AdminFeatureAction action) =>
        new(
            action.ActionCode,
            action.LabelKey,
            action.LabelFallback,
            action.PermissionCode,
            action.SortOrder);

    private static DynamicEndpointSchemaDto? ToEndpointSchema(IReadOnlyList<AdminFeatureApi> apis)
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

    private static IReadOnlyList<DynamicFieldOptionDto>? ParseOptions(string? optionsJson)
    {
        if (string.IsNullOrWhiteSpace(optionsJson))
        {
            return null;
        }

        return JsonSerializer.Deserialize<IReadOnlyList<DynamicFieldOptionDto>>(optionsJson, JsonOptions);
    }

    private static JsonElement? ParseValidation(string? validationJson)
    {
        if (string.IsNullOrWhiteSpace(validationJson))
        {
            return null;
        }

        return JsonSerializer.Deserialize<JsonElement>(validationJson, JsonOptions);
    }

    private static string NormalizeFeatureCode(string featureCode)
    {
        var normalized = featureCode.Trim();
        return string.IsNullOrWhiteSpace(normalized)
            ? throw new ValidationDomainException("FeatureCode is required.")
            : normalized;
    }
}
