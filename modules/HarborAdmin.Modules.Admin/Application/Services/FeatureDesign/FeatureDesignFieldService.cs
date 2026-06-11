using HarborAdmin.Modules.Admin.Contracts.FeatureDesign.Dto;
using HarborAdmin.BuildingBlocks.Abstractions.Exception;
using HarborAdmin.Modules.Admin.Contracts.FeatureDesign.Request;
using HarborAdmin.Modules.Admin.Domain.Entities;

namespace HarborAdmin.Modules.Admin.Application.Services.FeatureDesign;

public sealed class FeatureDesignFieldService
{
    private readonly FeatureDesignServiceContext _context;

    /// <summary>
    /// 初始化功能字段服务。
    /// </summary>
    public FeatureDesignFieldService(FeatureDesignServiceContext context)
    {
        _context = context;
    }

    /// <summary>
    /// 查询功能字段。
    /// </summary>
    public async Task<IReadOnlyList<AdminFeatureFieldDto>> ListFieldsAsync(string featureCode, CancellationToken cancellationToken)
    {
        var feature = _context.EnsureFeatureNode(await _context.LoadFeatureAggregateAsync(featureCode, cancellationToken)
                          ?? throw new NotFoundDomainException($"Feature '{featureCode}' was not found."));
        var fields = feature.Fields
            .OrderBy(item => item.SortOrder)
            .ToArray();
        return _context.Mapper.Map<AdminFeatureFieldDto[]>(fields);
    }

    /// <summary>
    /// 新建字段。
    /// </summary>
    public async Task<AdminFeatureFieldDto> CreateFieldAsync(string featureCode, SaveAdminFeatureFieldRequest request, CancellationToken cancellationToken)
    {
        var feature = _context.EnsureFeatureNode(await _context.LoadFeatureAggregateAsync(featureCode, cancellationToken)
                          ?? throw new NotFoundDomainException($"Feature '{featureCode}' was not found."));
        var normalized = feature.FeatureCode;
        var fieldCode = request.FieldCode.Trim();
        if (feature.Fields.Any(item => string.Equals(item.FieldCode, fieldCode, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ConflictDomainException($"Feature field '{normalized}.{fieldCode}' already exists.");
        }

        var now = DateTimeOffset.UtcNow;
        var field = new AdminFeatureField
        {
            AdminFeatureId = feature.Id,
            FeatureCode = normalized,
            FieldCode = fieldCode,
            CreatedAt = now,
        };
        ApplyField(field, request, now);
        feature.Fields.Add(field);
        _context.SaveFeatureChildren(feature, nameof(AdminFeature.Fields));
        await _context.IncrementSchemaVersionAsync(normalized, cancellationToken);
        await _context.AdminContext.BumpSessionVersionAsync(cancellationToken);
        return _context.Mapper.Map<AdminFeatureFieldDto>(field);
    }

    /// <summary>
    /// 更新字段。
    /// </summary>
    public async Task<AdminFeatureFieldDto> UpdateFieldAsync(string featureCode, string fieldCode, SaveAdminFeatureFieldRequest request, CancellationToken cancellationToken)
    {
        var feature = _context.EnsureFeatureNode(await _context.LoadFeatureAggregateAsync(featureCode, cancellationToken)
                          ?? throw new NotFoundDomainException($"Feature '{featureCode}' was not found."));
        var normalized = feature.FeatureCode;
        var normalizedField = fieldCode.Trim();
        var field = feature.Fields.FirstOrDefault(item => string.Equals(item.FieldCode, normalizedField, StringComparison.OrdinalIgnoreCase))
                    ?? throw new NotFoundDomainException($"Feature field '{normalized}.{normalizedField}' was not found.");
        field.AdminFeatureId = feature.Id;
        ApplyField(field, request, DateTimeOffset.UtcNow);
        _context.SaveFeatureChildren(feature, nameof(AdminFeature.Fields));
        await _context.IncrementSchemaVersionAsync(normalized, cancellationToken);
        await _context.AdminContext.BumpSessionVersionAsync(cancellationToken);
        return _context.Mapper.Map<AdminFeatureFieldDto>(field);
    }

    /// <summary>
    /// 排序字段。
    /// </summary>
    public async Task ReorderFieldsAsync(string featureCode, ReorderAdminFeatureFieldRequest request, CancellationToken cancellationToken)
    {
        var feature = _context.EnsureFeatureNode(await _context.LoadFeatureAggregateAsync(featureCode, cancellationToken)
                          ?? throw new NotFoundDomainException($"Feature '{featureCode}' was not found."));
        var fields = feature.Fields
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.FieldCode)
            .ToArray();
        var fieldIds = fields.Select(item => item.Id).ToHashSet();
        if (fields.Length != request.OrderedIds!.Count || request.OrderedIds.Any(id => !fieldIds.Contains(id)))
        {
            throw new ValidationDomainException("只能在当前功能的字段内排序。");
        }

        var orderedIndex = request.OrderedIds
            .Select((id, index) => new { id, index })
            .ToDictionary(item => item.id, item => item.index);
        var now = DateTimeOffset.UtcNow;
        foreach (var field in fields)
        {
            field.SortOrder = (orderedIndex[field.Id] + 1) * 10;
            field.UpdatedAt = now;
        }

        await _context.Repository.UpdateFeatureFieldsAsync(fields, cancellationToken);
        await _context.IncrementSchemaVersionAsync(feature.FeatureCode, cancellationToken);
        await _context.AdminContext.BumpSessionVersionAsync(cancellationToken);
    }

    /// <summary>
    /// 删除字段。
    /// </summary>
    public async Task DeleteFieldAsync(string featureCode, string fieldCode, CancellationToken cancellationToken)
    {
        var feature = _context.EnsureFeatureNode(await _context.LoadFeatureAggregateAsync(featureCode, cancellationToken)
                          ?? throw new NotFoundDomainException($"Feature '{featureCode}' was not found."));
        var normalized = feature.FeatureCode;
        var normalizedField = fieldCode.Trim();
        var field = feature.Fields.FirstOrDefault(item => string.Equals(item.FieldCode, normalizedField, StringComparison.OrdinalIgnoreCase))
                    ?? throw new NotFoundDomainException($"Feature field '{normalized}.{normalizedField}' was not found.");
        var fieldId = field.Id;
        feature.Fields.Remove(field);
        _context.SaveFeatureChildren(feature, nameof(AdminFeature.Fields));
        await _context.Repository.DeleteRoleFieldPermissionLinksByFieldIdAsync(fieldId, cancellationToken);
        await _context.IncrementSchemaVersionAsync(normalized, cancellationToken);
        await _context.AdminContext.BumpSessionVersionAsync(cancellationToken);
    }

    /// <summary>
    /// 将字段配置请求归一化后写回字段实体。
    /// </summary>
    private static void ApplyField(AdminFeatureField field, SaveAdminFeatureFieldRequest request, DateTimeOffset now)
    {
        field.FieldCode = request.FieldCode.Trim();
        field.LabelKey = request.LabelKey.Trim();
        field.LabelFallback = string.IsNullOrWhiteSpace(request.LabelFallback) ? null : request.LabelFallback.Trim();
        field.PlaceholderKey = string.IsNullOrWhiteSpace(request.PlaceholderKey) ? null : request.PlaceholderKey.Trim();
        field.PlaceholderFallback = string.IsNullOrWhiteSpace(request.PlaceholderFallback) ? null : request.PlaceholderFallback.Trim();
        field.Component = request.Component;
        field.DataType = request.DataType;
        field.ListVisible = request.ListVisible;
        field.SearchVisible = request.SearchVisible;
        field.CreateVisible = request.CreateVisible;
        field.UpdateVisible = request.UpdateVisible;
        field.Readonly = request.Readonly;
        field.Required = request.Required;
        field.SortOrder = request.SortOrder;
        // FreeSql/前端 schema 均用 null 表示未指定宽度，避免把非法宽度持久化为 0。
        field.Width = request.Width is <= 0 ? null : request.Width;
        field.DictCode = string.IsNullOrWhiteSpace(request.DictCode) ? null : request.DictCode.Trim();
        field.OptionsJson = string.IsNullOrWhiteSpace(field.DictCode) && !string.IsNullOrWhiteSpace(request.OptionsJson)
            ? request.OptionsJson
            : null;
        field.ValidationJson = string.IsNullOrWhiteSpace(request.ValidationJson) ? null : request.ValidationJson;
        field.Enabled = request.Enabled;
        field.UpdatedAt = now;
    }
}
