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
        var feature = _context.EnsureFeatureNode(await _context.LoadFeatureAggregateAsync(featureCode, cancellationToken)
                          ?? throw new NotFoundDomainException($"Feature '{featureCode}' was not found."));
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
        var feature = _context.EnsureFeatureNode(await _context.LoadFeatureAggregateAsync(featureCode, cancellationToken)
                          ?? throw new NotFoundDomainException($"Feature '{featureCode}' was not found."));
        var normalized = feature.FeatureCode;
        var actionCode = request.ActionCode.Trim();
        var permissionCode = request.PermissionCode.Trim();
        if (feature.Actions.Any(item => string.Equals(item.ActionCode, actionCode, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ConflictDomainException($"Feature action '{normalized}.{actionCode}' already exists.");
        }
        if (await _context.Repository.PermissionCodeExistsAsync(permissionCode, cancellationToken: cancellationToken))
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
        var feature = _context.EnsureFeatureNode(await _context.LoadFeatureAggregateAsync(featureCode, cancellationToken)
                          ?? throw new NotFoundDomainException($"Feature '{featureCode}' was not found."));
        var normalized = feature.FeatureCode;
        var normalizedAction = actionCode.Trim();
        var action = feature.Actions.FirstOrDefault(item => string.Equals(item.ActionCode, normalizedAction, StringComparison.OrdinalIgnoreCase))
                     ?? throw new NotFoundDomainException($"Feature action '{normalized}.{normalizedAction}' was not found.");
        var oldPermissionCode = action.PermissionCode;
        var requestedPermissionCode = request.PermissionCode.Trim();

        if (!string.Equals(oldPermissionCode, requestedPermissionCode, StringComparison.OrdinalIgnoreCase)
            && await _context.Repository.PermissionCodeExistsAsync(requestedPermissionCode, action.Id, cancellationToken))
        {
            throw new ConflictDomainException("权限编码已存在，请使用不同的权限码。");
        }

        action.AdminFeatureId = feature.Id;
        ApplyAction(action, request, DateTimeOffset.UtcNow);
        _context.SaveFeatureChildren(feature, nameof(AdminFeature.Actions));
        if (!string.Equals(oldPermissionCode, action.PermissionCode, StringComparison.OrdinalIgnoreCase))
        {
            await _context.Repository.UpdateRolePermissionCodeAsync(action.Id, action.PermissionCode, cancellationToken);
        }

        await ReplaceActionApisAsync(normalized, action.ActionCode, request.ApiIds ?? [], cancellationToken);
        await _context.IncrementSchemaVersionAsync(normalized, cancellationToken);
        await _context.AdminContext.BumpSessionVersionAsync(cancellationToken);
        var saved = await _context.LoadActionAsync(normalized, action.ActionCode, cancellationToken);
        return MapAction(saved);
    }

    /// <summary>
    /// 排序权限点。
    /// </summary>
    public async Task ReorderActionsAsync(string featureCode, ReorderAdminFeatureActionRequest request, CancellationToken cancellationToken)
    {
        var feature = _context.EnsureFeatureNode(await _context.LoadFeatureAggregateAsync(featureCode, cancellationToken)
                          ?? throw new NotFoundDomainException($"Feature '{featureCode}' was not found."));
        var actions = feature.Actions
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.ActionCode)
            .ToArray();
        var actionIds = actions.Select(item => item.Id).ToHashSet();
        if (actions.Length != request.OrderedIds!.Count || request.OrderedIds.Any(id => !actionIds.Contains(id)))
        {
            throw new ValidationDomainException("只能在当前功能的权限点内排序。");
        }

        var orderedIndex = request.OrderedIds
            .Select((id, index) => new { id, index })
            .ToDictionary(item => item.id, item => item.index);
        var now = DateTimeOffset.UtcNow;
        foreach (var action in actions)
        {
            action.SortOrder = (orderedIndex[action.Id] + 1) * 10;
            action.UpdatedAt = now;
        }

        await _context.Repository.UpdateFeatureActionsAsync(actions, cancellationToken);
        await _context.IncrementSchemaVersionAsync(feature.FeatureCode, cancellationToken);
        await _context.AdminContext.BumpSessionVersionAsync(cancellationToken);
    }

    /// <summary>
    /// 删除动作。
    /// </summary>
    public async Task DeleteActionAsync(string featureCode, string actionCode, CancellationToken cancellationToken)
    {
        var feature = _context.EnsureFeatureNode(await _context.LoadFeatureAggregateAsync(featureCode, cancellationToken)
                          ?? throw new NotFoundDomainException($"Feature '{featureCode}' was not found."));
        var normalized = feature.FeatureCode;
        var normalizedAction = actionCode.Trim();
        var action = feature.Actions.FirstOrDefault(item => string.Equals(item.ActionCode, normalizedAction, StringComparison.OrdinalIgnoreCase))
                     ?? throw new NotFoundDomainException($"Feature action '{normalized}.{normalizedAction}' was not found.");
        action.ActionApis.Clear();
        _context.SaveActionChildren(action, nameof(AdminFeatureAction.ActionApis));
        await _context.Repository.DeleteRolePermissionLinksByActionIdAsync(action.Id, cancellationToken);
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
        var feature = _context.EnsureFeatureNode(await _context.LoadFeatureAggregateAsync(featureCode, cancellationToken)
                          ?? throw new NotFoundDomainException($"Feature '{featureCode}' was not found."));
        var normalized = feature.FeatureCode;
        var normalizedAction = actionCode.Trim();
        var action = feature.Actions.FirstOrDefault(item => string.Equals(item.ActionCode, normalizedAction, StringComparison.OrdinalIgnoreCase))
                     ?? throw new NotFoundDomainException($"Feature action '{normalized}.{normalizedAction}' was not found.");
        await ReplaceActionApisAsync(normalized, action.ActionCode, apiIds, cancellationToken);
        await _context.AdminContext.BumpSessionVersionAsync(cancellationToken);
        var saved = await _context.LoadActionAsync(normalized, action.ActionCode, cancellationToken);
        return MapAction(saved);
    }

    /// <summary>
    /// 整体替换动作绑定的 API 列表。
    /// </summary>
    private async Task ReplaceActionApisAsync(string featureCode, string actionCode, IReadOnlyList<long> apiIds, CancellationToken cancellationToken)
    {
        var feature = _context.EnsureFeatureNode(await _context.LoadFeatureAggregateAsync(featureCode, cancellationToken)
                          ?? throw new NotFoundDomainException($"Feature '{featureCode}' was not found."));
        var action = feature.Actions.FirstOrDefault(item => string.Equals(item.ActionCode, actionCode, StringComparison.OrdinalIgnoreCase))
                      ?? throw new NotFoundDomainException($"Feature action '{feature.FeatureCode}.{actionCode}' was not found.");
        var distinctApiIds = apiIds
            .Where(item => item > 0)
            .Distinct()
            .ToArray();

        // 绑定列表按请求整体替换，先删旧关系再插入新关系。
        await _context.Repository.DeleteActionApiLinksAsync(action.Id, cancellationToken);

        if (distinctApiIds.Length == 0)
        {
            return;
        }

        var validApis = await _context.Repository.GetFeatureApisByIdsAsync(distinctApiIds, cancellationToken);
        if (validApis.Count != distinctApiIds.Length)
        {
            throw new ValidationDomainException("动作只能绑定已存在的 API。");
        }

        action.ActionApis = validApis.Select(api => new AdminFeatureActionApi
        {
            AdminFeatureId = feature.Id,
            AdminFeatureActionId = action.Id,
            AdminFeatureApiId = api.Id,
            FeatureCode = feature.FeatureCode,
            ActionCode = action.ActionCode,
        }).ToList();

        await _context.Repository.InsertActionApiLinksAsync(action.ActionApis, cancellationToken);
    }

    /// <summary>
    /// 映射动作 DTO 并附加已绑定 API ID。
    /// </summary>
    private AdminFeatureActionDto MapAction(AdminFeatureAction action) =>
        _context.Mapper.Map<AdminFeatureActionDto>(
            new AdminFeatureActionMappingSource(
                action,
                action.ActionApis
                    .Select(link => link.AdminFeatureApiId)
                    .ToArray()));

    /// <summary>
    /// 将请求值应用到动作实体。
    /// </summary>
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
