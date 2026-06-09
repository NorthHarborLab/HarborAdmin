using HarborAdmin.BuildingBlocks.Abstractions.Exception;
using HarborAdmin.Modules.Admin.Contracts.FeatureDesign;
using HarborAdmin.Modules.Admin.Contracts.FeatureDesign.Dto;
using HarborAdmin.Modules.Admin.Contracts.FeatureDesign.Request;
using HarborAdmin.Modules.Admin.Domain.Entities;

namespace HarborAdmin.Modules.Admin.Application.Services.FeatureDesign;

public sealed class FeatureDesignFeatureService
{
    private const string UncategorizedFeatureCode = "__uncategorized";
    private readonly FeatureDesignServiceContext _context;

    /// <summary>
    /// 初始化功能页面服务。
    /// </summary>
    public FeatureDesignFeatureService(FeatureDesignServiceContext context)
    {
        _context = context;
    }

    /// <summary>
    /// 查询 Feature 树。
    /// </summary>
    public async Task<IReadOnlyList<AdminFeatureDto>> ListFeaturesAsync(CancellationToken cancellationToken)
    {
        var features = await _context.Db.Orm.Select<AdminFeature>()
            .OrderBy(item => item.ParentId)
            .OrderBy(item => item.SortOrder)
            .OrderBy(item => item.FeatureCode)
            .ToListAsync(cancellationToken);
        return BuildFeatureTree(features);
    }

    /// <summary>
    /// 新建 Feature。
    /// </summary>
    public async Task<AdminFeatureDto> CreateFeatureAsync(SaveAdminFeatureRequest request, CancellationToken cancellationToken)
    {
        var featureCode = request.FeatureCode.Trim();
        if (featureCode.Equals(UncategorizedFeatureCode, StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationDomainException("功能编码不能使用系统保留值。");
        }

        if (await _context.Db.Orm.Select<AdminFeature>().Where(item => item.FeatureCode == featureCode).AnyAsync(cancellationToken))
        {
            throw new ConflictDomainException($"Feature '{featureCode}' already exists.");
        }

        var now = DateTimeOffset.UtcNow;
        var feature = new AdminFeature
        {
            FeatureCode = featureCode,
            CreatedAt = now,
        };
        await ApplyFeatureAsync(feature, request, now, isNew: true, cancellationToken);
        var repository = _context.GetFeatureRepository();
        repository.DbContextOptions.EnableCascadeSave = true;
        await repository.InsertAsync(feature, cancellationToken);
        await _context.AdminContext.BumpSessionVersionAsync(cancellationToken);
        return MapFeature(feature);
    }

    /// <summary>
    /// 更新 Feature。
    /// </summary>
    public async Task<AdminFeatureDto> UpdateFeatureAsync(string featureCode, SaveAdminFeatureRequest request, CancellationToken cancellationToken)
    {
        var normalized = featureCode.Trim();
        var nextFeatureCode = request.FeatureCode.Trim();
        if (nextFeatureCode.Equals(UncategorizedFeatureCode, StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationDomainException("功能编码不能使用系统保留值。");
        }

        var feature = await _context.Db.Orm.Select<AdminFeature>().Where(item => item.FeatureCode == normalized).ToOneAsync(cancellationToken)
                      ?? throw new NotFoundDomainException($"Feature '{normalized}' was not found.");
        if (!nextFeatureCode.Equals(normalized, StringComparison.OrdinalIgnoreCase)
            && await _context.Db.Orm.Select<AdminFeature>().Where(item => item.FeatureCode == nextFeatureCode).AnyAsync(cancellationToken))
        {
            throw new ConflictDomainException($"Feature '{nextFeatureCode}' already exists.");
        }

        await ApplyFeatureAsync(feature, request, DateTimeOffset.UtcNow, isNew: false, cancellationToken);
        await _context.GetFeatureRepository().UpdateAsync(feature, cancellationToken);
        await _context.AdminContext.BumpSessionVersionAsync(cancellationToken);
        return MapFeature(feature);
    }

    /// <summary>
    /// 同组排序 Feature。
    /// </summary>
    public async Task ReorderFeaturesAsync(ReorderAdminFeatureRequest request, CancellationToken cancellationToken)
    {
        var siblings = await LoadSortableSiblingsAsync(request, cancellationToken);
        var siblingIds = siblings.Select(item => item.Id).ToHashSet();
        if (siblings.Count != request.OrderedIds!.Count || request.OrderedIds.Any(id => !siblingIds.Contains(id)))
        {
            throw new ValidationDomainException("只能在同一分组内排序。");
        }

        var orderedIndex = request.OrderedIds
            .Select((id, index) => new { id, index })
            .ToDictionary(item => item.id, item => item.index);
        var now = DateTimeOffset.UtcNow;
        foreach (var sibling in siblings)
        {
            sibling.SortOrder = (orderedIndex[sibling.Id] + 1) * 10;
            sibling.UpdatedAt = now;
        }

        await _context.Db.Orm.Update<AdminFeature>().SetSource(siblings).ExecuteAffrowsAsync(cancellationToken);
        await _context.AdminContext.BumpSessionVersionAsync(cancellationToken);
    }

    /// <summary>
    /// 删除 Feature。
    /// </summary>
    public async Task DeleteFeatureAsync(string featureCode, CancellationToken cancellationToken)
    {
        var normalized = featureCode.Trim();
        var feature = await _context.LoadFeatureAggregateAsync(normalized, cancellationToken)
                      ?? throw new NotFoundDomainException($"Feature '{normalized}' was not found.");
        if (IsCategoryNode(feature))
        {
            var childCount = await _context.Db.Orm.Select<AdminFeature>()
                .Where(item => item.ParentId == feature.Id)
                .CountAsync(cancellationToken);
            if (childCount > 0)
            {
                throw new ConflictDomainException("分类下存在子节点，不能删除。");
            }
        }

        var usedByMenu = await _context.Db.Orm.Select<AdminMenu>()
            .Where(menu => menu.AdminFeatureId == feature.Id || menu.FeatureCode == normalized)
            .AnyAsync(cancellationToken);
        if (usedByMenu)
        {
            throw new ConflictDomainException("功能已被菜单引用，不能删除。");
        }

        var actionIds = feature.Actions.Select(item => item.Id).ToArray();
        if (actionIds.Length > 0)
        {
            await _context.Db.Orm.Delete<AdminRolePermission>().Where(item => actionIds.Contains(item.AdminFeatureActionId)).ExecuteAffrowsAsync(cancellationToken);
        }

        var fieldIds = feature.Fields.Select(item => item.Id).ToArray();
        if (fieldIds.Length > 0)
        {
            await _context.Db.Orm.Delete<AdminRoleFieldPermission>().Where(item => fieldIds.Contains(item.AdminFeatureFieldId)).ExecuteAffrowsAsync(cancellationToken);
        }

        await _context.GetFeatureRepository().DeleteCascadeByDatabaseAsync(item => item.Id == feature.Id, cancellationToken);
        await _context.AdminContext.BumpSessionVersionAsync(cancellationToken);
    }

    /// <summary>
    /// 将保存请求归一化后写回 Feature 聚合根。
    /// </summary>
    private async Task ApplyFeatureAsync(AdminFeature feature, SaveAdminFeatureRequest request, DateTimeOffset now, bool isNew, CancellationToken cancellationToken)
    {
        feature.FeatureCode = request.FeatureCode.Trim();
        feature.Name = string.IsNullOrWhiteSpace(request.Name)
            ? throw new ValidationDomainException("名称不能为空。")
            : request.Name.Trim();
        feature.NodeType = request.NodeType;
        feature.ParentId = await ResolveParentIdAsync(request.ParentId, isNew ? null : feature.Id, cancellationToken);
        feature.SortOrder = request.SortOrder;
        feature.Enabled = request.Enabled;
        feature.UpdatedAt = now;

        if (IsCategoryNode(feature))
        {
            feature.FeatureType = AdminFeatureType.Static;
            feature.Component = string.Empty;
            feature.HandlerKey = null;
            feature.RoutePath = null;
            return;
        }

        if (string.IsNullOrWhiteSpace(request.Component))
        {
            throw new ValidationDomainException("功能组件不能为空。");
        }

        var featureType = request.FeatureType;
        feature.FeatureType = featureType;
        feature.Component = request.Component.Trim();
        feature.HandlerKey = featureType == AdminFeatureType.Dynamic && !string.IsNullOrWhiteSpace(request.HandlerKey)
            ? request.HandlerKey.Trim()
            : null;
        feature.RoutePath = string.IsNullOrWhiteSpace(request.RoutePath) ? null : request.RoutePath.Trim();
    }

    private async Task<long?> ResolveParentIdAsync(long? parentId, long? currentFeatureId, CancellationToken cancellationToken)
    {
        if (!parentId.HasValue)
        {
            return null;
        }

        if (currentFeatureId == parentId.Value)
        {
            throw new ValidationDomainException("父级分类不能选择当前节点。");
        }

        var parent = await _context.Db.Orm.Select<AdminFeature>().Where(item => item.Id == parentId.Value).ToOneAsync(cancellationToken)
                     ?? throw new NotFoundDomainException("父级分类不存在。");
        if (!IsCategoryNode(parent))
        {
            throw new ValidationDomainException("父级只能选择分类节点。");
        }

        if (currentFeatureId.HasValue && await IsDescendantAsync(parent.Id, currentFeatureId.Value, cancellationToken))
        {
            throw new ValidationDomainException("父级分类不能选择当前节点的子孙节点。");
        }

        return parent.Id;
    }

    private async Task<List<AdminFeature>> LoadSortableSiblingsAsync(ReorderAdminFeatureRequest request, CancellationToken cancellationToken)
    {
        if (request.ParentId.HasValue)
        {
            var parent = await _context.Db.Orm.Select<AdminFeature>()
                .Where(item => item.Id == request.ParentId.Value)
                .ToOneAsync(cancellationToken)
                ?? throw new NotFoundDomainException("父级分类不存在。");
            if (!IsCategoryNode(parent))
            {
                throw new ValidationDomainException("父级只能选择分类节点。");
            }

            return await _context.Db.Orm.Select<AdminFeature>()
                .Where(item => item.ParentId == parent.Id)
                .OrderBy(item => item.SortOrder)
                .OrderBy(item => item.FeatureCode)
                .ToListAsync(cancellationToken);
        }

        var nodeType = request.NodeType ?? AdminFeatureNodeType.Feature;
        if (nodeType == AdminFeatureNodeType.Category)
        {
            return await _context.Db.Orm.Select<AdminFeature>()
                .Where(item => item.ParentId == null && item.NodeType == AdminFeatureNodeType.Category)
                .OrderBy(item => item.SortOrder)
                .OrderBy(item => item.FeatureCode)
                .ToListAsync(cancellationToken);
        }

        return await _context.Db.Orm.Select<AdminFeature>()
            .Where(item => item.ParentId == null && item.NodeType != AdminFeatureNodeType.Category)
            .OrderBy(item => item.SortOrder)
            .OrderBy(item => item.FeatureCode)
            .ToListAsync(cancellationToken);
    }

    private async Task<bool> IsDescendantAsync(long candidateParentId, long currentFeatureId, CancellationToken cancellationToken)
    {
        var nextParentId = candidateParentId;
        while (true)
        {
            if (nextParentId == currentFeatureId)
            {
                return true;
            }

            var next = await _context.Db.Orm.Select<AdminFeature>()
                .Where(item => item.Id == nextParentId)
                .ToOneAsync(cancellationToken);
            if (next?.ParentId is null)
            {
                return false;
            }

            nextParentId = next.ParentId.Value;
        }
    }

    private IReadOnlyList<AdminFeatureDto> BuildFeatureTree(IReadOnlyList<AdminFeature> features)
    {
        var childrenByParent = features
            .Where(item => item.ParentId.HasValue)
            .GroupBy(item => item.ParentId!.Value)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderBy(item => item.SortOrder)
                    .ThenBy(item => item.FeatureCode, StringComparer.OrdinalIgnoreCase)
                    .ToArray());
        var roots = features
            .Where(item => !item.ParentId.HasValue)
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.FeatureCode, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var result = new List<AdminFeatureDto>();
        var uncategorized = new List<AdminFeatureDto>();

        foreach (var root in roots)
        {
            if (IsCategoryNode(root))
            {
                result.Add(MapTreeNode(root, childrenByParent));
            }
            else
            {
                uncategorized.Add(MapTreeNode(root, childrenByParent));
            }
        }

        if (uncategorized.Count > 0)
        {
            result.Add(new AdminFeatureDto(
                -1,
                null,
                UncategorizedFeatureCode,
                "未分类",
                AdminFeatureType.Static,
                AdminFeatureNodeType.Category,
                string.Empty,
                null,
                null,
                1,
                true,
                int.MaxValue,
                DateTimeOffset.MinValue,
                null,
                true)
            {
                Children = uncategorized,
            });
        }

        return result;
    }

    private AdminFeatureDto MapTreeNode(AdminFeature feature, IReadOnlyDictionary<long, AdminFeature[]> childrenByParent) =>
        MapFeature(feature) with
        {
            Children = childrenByParent.GetValueOrDefault(feature.Id) is { } children
                ? children
                    .OrderBy(item => item.SortOrder)
                    .ThenBy(item => item.FeatureCode, StringComparer.OrdinalIgnoreCase)
                    .Select(child => MapTreeNode(child, childrenByParent))
                    .ToArray()
                : [],
        };

    private AdminFeatureDto MapFeature(AdminFeature feature)
    {
        var dto = _context.Mapper.Map<AdminFeatureDto>(feature);
        return dto;
    }

    private static bool IsCategoryNode(AdminFeature feature) =>
        feature.NodeType == AdminFeatureNodeType.Category;
}
