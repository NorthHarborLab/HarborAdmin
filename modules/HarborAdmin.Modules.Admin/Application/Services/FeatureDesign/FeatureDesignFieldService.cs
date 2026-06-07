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
        var feature = await _context.LoadFeatureAggregateAsync(featureCode, cancellationToken)
                      ?? throw new NotFoundDomainException($"Feature '{featureCode}' was not found.");
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
        var feature = await _context.LoadFeatureAggregateAsync(featureCode, cancellationToken)
                      ?? throw new NotFoundDomainException($"Feature '{featureCode}' was not found.");
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
        var feature = await _context.LoadFeatureAggregateAsync(featureCode, cancellationToken)
                      ?? throw new NotFoundDomainException($"Feature '{featureCode}' was not found.");
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
    /// 删除字段。
    /// </summary>
    public async Task DeleteFieldAsync(string featureCode, string fieldCode, CancellationToken cancellationToken)
    {
        var feature = await _context.LoadFeatureAggregateAsync(featureCode, cancellationToken)
                      ?? throw new NotFoundDomainException($"Feature '{featureCode}' was not found.");
        var normalized = feature.FeatureCode;
        var normalizedField = fieldCode.Trim();
        var field = feature.Fields.FirstOrDefault(item => string.Equals(item.FieldCode, normalizedField, StringComparison.OrdinalIgnoreCase))
                    ?? throw new NotFoundDomainException($"Feature field '{normalized}.{normalizedField}' was not found.");
        var fieldId = field.Id;
        feature.Fields.Remove(field);
        _context.SaveFeatureChildren(feature, nameof(AdminFeature.Fields));
        await _context.Db.Orm.Delete<AdminRoleFieldPermission>()
            .Where(item => item.AdminFeatureFieldId == fieldId)
            .ExecuteAffrowsAsync(cancellationToken);
        await _context.IncrementSchemaVersionAsync(normalized, cancellationToken);
        await _context.AdminContext.BumpSessionVersionAsync(cancellationToken);
    }

    private static void ApplyField(AdminFeatureField field, SaveAdminFeatureFieldRequest request, DateTimeOffset now)
    {
        field.FieldCode = request.FieldCode.Trim();
        field.LabelKey = request.LabelKey.Trim();
        field.LabelFallback = string.IsNullOrWhiteSpace(request.LabelFallback) ? null : request.LabelFallback.Trim();
        field.PlaceholderKey = string.IsNullOrWhiteSpace(request.PlaceholderKey) ? null : request.PlaceholderKey.Trim();
        field.PlaceholderFallback = string.IsNullOrWhiteSpace(request.PlaceholderFallback) ? null : request.PlaceholderFallback.Trim();
        field.Component = request.Component.Trim();
        field.DataType = request.DataType.Trim();
        field.ListVisible = request.ListVisible;
        field.SearchVisible = request.SearchVisible;
        field.CreateVisible = request.CreateVisible;
        field.UpdateVisible = request.UpdateVisible;
        field.Readonly = request.Readonly;
        field.Required = request.Required;
        field.SortOrder = request.SortOrder;
        field.Width = request.Width is <= 0 ? null : request.Width;
        field.OptionsJson = string.IsNullOrWhiteSpace(request.OptionsJson) ? null : request.OptionsJson;
        field.ValidationJson = string.IsNullOrWhiteSpace(request.ValidationJson) ? null : request.ValidationJson;
        field.Enabled = request.Enabled;
        field.UpdatedAt = now;
    }
}
