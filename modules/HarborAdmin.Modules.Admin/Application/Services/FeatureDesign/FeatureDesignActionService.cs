using HarborAdmin.BuildingBlocks.Abstractions.Exception;
using HarborAdmin.Modules.Admin.Application.Mappings;
using HarborAdmin.Modules.Admin.Contracts.FeatureDesign.Dto;
using HarborAdmin.Modules.Admin.Contracts.FeatureDesign.Request;
using HarborAdmin.Modules.Admin.Domain.Entities;

namespace HarborAdmin.Modules.Admin.Application.Services.FeatureDesign;

public sealed class FeatureDesignActionService
{
    private readonly FeatureDesignServiceContext _context;

    /// <summary>
    /// 初始化功能动作服务。
    /// </summary>
    public FeatureDesignActionService(FeatureDesignServiceContext context)
    {
        _context = context;
    }

    /// <summary>
    /// 查询功能动作。
    /// </summary>
    public async Task<IReadOnlyList<AdminFeatureActionDto>> ListActionsAsync(string featureCode, CancellationToken cancellationToken)
    {
        var feature = await _context.LoadFeatureAggregateAsync(featureCode, cancellationToken)
                      ?? throw new NotFoundDomainException($"Feature '{featureCode}' was not found.");
        return feature.Actions
            .OrderBy(item => item.SortOrder)
            .Select(MapAction)
            .ToArray();
    }

    /// <summary>
    /// 新建按钮。
    /// </summary>
    public async Task<AdminFeatureActionDto> CreateActionAsync(string featureCode, SaveAdminFeatureActionRequest request, CancellationToken cancellationToken)
    {
        var feature = await _context.LoadFeatureAggregateAsync(featureCode, cancellationToken)
                      ?? throw new NotFoundDomainException($"Feature '{featureCode}' was not found.");
        var normalized = feature.FeatureCode;
        var actionCode = request.ActionCode.Trim();
        var permissionCode = request.PermissionCode.Trim();
        if (feature.Actions.Any(item => string.Equals(item.ActionCode, actionCode, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ConflictDomainException($"Feature action '{normalized}.{actionCode}' already exists.");
        }
        if (await _context.Db.Orm.Select<AdminFeatureAction>()
                .Where(item => item.PermissionCode == permissionCode)
                .AnyAsync(cancellationToken))
        {
            throw new ConflictDomainException("权限编码已存在，请使用不同的权限码。");
        }

        var now = DateTimeOffset.UtcNow;
        var action = new AdminFeatureAction
        {
            AdminFeatureId = feature.Id,
            FeatureCode = normalized,
            ActionCode = actionCode,
            CreatedAt = now,
        };
        ApplyAction(action, request, now);
        feature.Actions.Add(action);
        _context.SaveFeatureChildren(feature, nameof(AdminFeature.Actions));
        await ReplaceActionApisAsync(normalized, action.ActionCode, request.ApiIds ?? [], cancellationToken);
        await _context.IncrementSchemaVersionAsync(normalized, cancellationToken);
        await _context.AdminContext.BumpSessionVersionAsync(cancellationToken);
        var saved = await _context.LoadActionAsync(normalized, action.ActionCode, cancellationToken);
        return MapAction(saved);
    }

    /// <summary>
    /// 更新动作。
    /// </summary>
    public async Task<AdminFeatureActionDto> UpdateActionAsync(string featureCode, string actionCode, SaveAdminFeatureActionRequest request, CancellationToken cancellationToken)
    {
        var feature = await _context.LoadFeatureAggregateAsync(featureCode, cancellationToken)
                      ?? throw new NotFoundDomainException($"Feature '{featureCode}' was not found.");
        var normalized = feature.FeatureCode;
        var normalizedAction = actionCode.Trim();
        var action = feature.Actions.FirstOrDefault(item => string.Equals(item.ActionCode, normalizedAction, StringComparison.OrdinalIgnoreCase))
                     ?? throw new NotFoundDomainException($"Feature action '{normalized}.{normalizedAction}' was not found.");
        var oldPermissionCode = action.PermissionCode;
        var requestedPermissionCode = request.PermissionCode.Trim();

        if (!string.Equals(oldPermissionCode, requestedPermissionCode, StringComparison.OrdinalIgnoreCase)
            && await _context.Db.Orm.Select<AdminFeatureAction>()
                .Where(item => item.PermissionCode == requestedPermissionCode)
                .Where(item => item.Id != action.Id)
                .AnyAsync(cancellationToken))
        {
            throw new ConflictDomainException("权限编码已存在，请使用不同的权限码。");
        }

        action.AdminFeatureId = feature.Id;
        ApplyAction(action, request, DateTimeOffset.UtcNow);
        _context.SaveFeatureChildren(feature, nameof(AdminFeature.Actions));
        if (!string.Equals(oldPermissionCode, action.PermissionCode, StringComparison.OrdinalIgnoreCase))
        {
            await _context.Db.Orm.Update<AdminRolePermission>()
                .Set(item => item.PermissionCode, action.PermissionCode)
                .Where(item => item.AdminFeatureActionId == action.Id)
                .ExecuteAffrowsAsync(cancellationToken);
        }

        await ReplaceActionApisAsync(normalized, action.ActionCode, request.ApiIds ?? [], cancellationToken);
        await _context.IncrementSchemaVersionAsync(normalized, cancellationToken);
        await _context.AdminContext.BumpSessionVersionAsync(cancellationToken);
        var saved = await _context.LoadActionAsync(normalized, action.ActionCode, cancellationToken);
        return MapAction(saved);
    }

    /// <summary>
    /// 删除动作。
    /// </summary>
    public async Task DeleteActionAsync(string featureCode, string actionCode, CancellationToken cancellationToken)
    {
        var feature = await _context.LoadFeatureAggregateAsync(featureCode, cancellationToken)
                      ?? throw new NotFoundDomainException($"Feature '{featureCode}' was not found.");
        var normalized = feature.FeatureCode;
        var normalizedAction = actionCode.Trim();
        var action = feature.Actions.FirstOrDefault(item => string.Equals(item.ActionCode, normalizedAction, StringComparison.OrdinalIgnoreCase))
                     ?? throw new NotFoundDomainException($"Feature action '{normalized}.{normalizedAction}' was not found.");
        action.ActionApis.Clear();
        _context.SaveActionChildren(action, nameof(AdminFeatureAction.ActionApis));
        await _context.Db.Orm.Delete<AdminRolePermission>().Where(item => item.AdminFeatureActionId == action.Id).ExecuteAffrowsAsync(cancellationToken);
        feature.Actions.Remove(action);
        _context.SaveFeatureChildren(feature, nameof(AdminFeature.Actions));
        await _context.IncrementSchemaVersionAsync(normalized, cancellationToken);
        await _context.AdminContext.BumpSessionVersionAsync(cancellationToken);
    }

    /// <summary>
    /// 保存动作 API 绑定。
    /// </summary>
    public async Task<AdminFeatureActionDto> SaveActionApisAsync(string featureCode, string actionCode, IReadOnlyList<long> apiIds, CancellationToken cancellationToken)
    {
        var feature = await _context.LoadFeatureAggregateAsync(featureCode, cancellationToken)
                      ?? throw new NotFoundDomainException($"Feature '{featureCode}' was not found.");
        var normalized = feature.FeatureCode;
        var normalizedAction = actionCode.Trim();
        var action = feature.Actions.FirstOrDefault(item => string.Equals(item.ActionCode, normalizedAction, StringComparison.OrdinalIgnoreCase))
                     ?? throw new NotFoundDomainException($"Feature action '{normalized}.{normalizedAction}' was not found.");
        await ReplaceActionApisAsync(normalized, action.ActionCode, apiIds, cancellationToken);
        await _context.AdminContext.BumpSessionVersionAsync(cancellationToken);
        var saved = await _context.LoadActionAsync(normalized, action.ActionCode, cancellationToken);
        return MapAction(saved);
    }

    private async Task ReplaceActionApisAsync(string featureCode, string actionCode, IReadOnlyList<long> apiIds, CancellationToken cancellationToken)
    {
        var feature = await _context.LoadFeatureAggregateAsync(featureCode, cancellationToken)
                      ?? throw new NotFoundDomainException($"Feature '{featureCode}' was not found.");
        var action = feature.Actions.FirstOrDefault(item => string.Equals(item.ActionCode, actionCode, StringComparison.OrdinalIgnoreCase))
                      ?? throw new NotFoundDomainException($"Feature action '{feature.FeatureCode}.{actionCode}' was not found.");
        var distinctApiIds = apiIds
            .Where(item => item > 0)
            .Distinct()
            .ToArray();

        await _context.Db.Orm.Delete<AdminFeatureActionApi>()
            .Where(item => item.AdminFeatureActionId == action.Id)
            .ExecuteAffrowsAsync(cancellationToken);

        if (distinctApiIds.Length == 0)
        {
            return;
        }

        var validApis = feature.Apis.Where(item => distinctApiIds.Contains(item.Id)).ToArray();
        if (validApis.Length != distinctApiIds.Length)
        {
            throw new ValidationDomainException("动作只能绑定当前 Feature 下已存在的 API。");
        }

        action.ActionApis = validApis.Select(api => new AdminFeatureActionApi
        {
            AdminFeatureId = feature.Id,
            AdminFeatureActionId = action.Id,
            AdminFeatureApiId = api.Id,
            FeatureCode = feature.FeatureCode,
            ActionCode = action.ActionCode,
        }).ToList();

        await _context.Db.Orm.Insert(action.ActionApis).ExecuteAffrowsAsync(cancellationToken);
    }

    private AdminFeatureActionDto MapAction(AdminFeatureAction action) =>
        _context.Mapper.Map<AdminFeatureActionDto>(
            new AdminFeatureActionMappingSource(
                action,
                action.ActionApis
                    .Select(link => link.AdminFeatureApiId)
                    .ToArray()));

    private static void ApplyAction(AdminFeatureAction action, SaveAdminFeatureActionRequest request, DateTimeOffset now)
    {
        action.ActionCode = request.ActionCode.Trim();
        action.PermissionCode = request.PermissionCode.Trim();
        action.LabelKey = request.LabelKey.Trim();
        action.LabelFallback = string.IsNullOrWhiteSpace(request.LabelFallback) ? null : request.LabelFallback.Trim();
        action.SortOrder = request.SortOrder;
        action.Enabled = request.Enabled;
        action.UpdatedAt = now;
    }
}
