using HarborAdmin.BuildingBlocks.Abstractions.Exception;
using HarborAdmin.Modules.Admin.Application.Abstractions;
using HarborAdmin.Modules.Admin.Contracts.System.Dto;
using HarborAdmin.Modules.Admin.Contracts.System.Request;
using HarborAdmin.Modules.Admin.Domain.Entities;
using HarborAdmin.Modules.Admin.Application.Services.Shared;

namespace HarborAdmin.Modules.Admin.Application.Services.Menu;

/// <summary>
/// 菜单管理服务。
/// </summary>
public sealed class MenuService(SystemServiceContext systemContext, AdminServiceContext context, IAdminRepository repository)
{
    /// <summary>
    /// 获取完整菜单树。
    /// </summary>
    public async Task<IReadOnlyList<SystemMenuDto>> ListMenusAsync(CancellationToken cancellationToken)
    {
        var menus = await repository.ListMenusWithFeaturesAsync(cancellationToken);
        var features = ExtractFeatures(menus);
        return MenuMapper.BuildSystemMenuTree(menus, features);
    }

    /// <summary>
    /// 获取包含按钮权限节点的菜单权限树。
    /// </summary>
    public async Task<IReadOnlyList<SystemMenuDto>> ListMenuPermissionTreeAsync(CancellationToken cancellationToken)
    {
        var menus = await repository.ListMenusWithFeaturesAsync(cancellationToken);
        var features = await repository.ListFeaturesAsync(cancellationToken);
        var actions = await repository.ListEnabledFeatureActionsAsync(cancellationToken);
        return MenuMapper.BuildSystemMenuTree(menus, features, actions);
    }

    /// <summary>
    /// 新增或更新菜单，并同步关联 Feature。
    /// </summary>
    public async Task<SystemMenuDto> SaveMenuAsync(long? id, SaveSystemMenuRequest request, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var menuType = MenuMapper.NormalizeMenuType(request.Type);
        var meta = MenuMapper.NormalizeMenuMeta(request, menuType);
        var routePath = MenuMapper.NormalizeMenuPath(request, meta, menuType);
        var featureCode = await MenuMapper.EnsureFeatureForMenuAsync(systemContext.Orm, request, menuType, cancellationToken);
        var feature = featureCode is null
            ? null
            : await systemContext.ResolveFeatureByCodeAsync(featureCode, cancellationToken)
              ?? throw new NotFoundDomainException($"未找到功能资源 '{featureCode}'。");
        var menu = id.HasValue
            ? await repository.GetMenuWithFeatureAsync(id.Value, cancellationToken)
              ?? throw new NotFoundDomainException("菜单不存在。")
            : new AdminMenu { CreatedAt = now };
        var parentId = AdminIdHelper.ParseNullableId(request.Pid);
        await EnsureValidParentAsync(id, parentId, cancellationToken);

        menu.MenuCode = MenuMapper.BuildMenuCode(menuType, routePath, request.AuthCode);
        menu.ParentId = parentId;
        menu.AdminFeatureId = feature?.Id;
        menu.FeatureCode = feature?.FeatureCode;
        menu.PermissionCode = string.IsNullOrWhiteSpace(request.AuthCode) ? null : request.AuthCode;
        menu.RoutePath = routePath;
        menu.RouteName = MenuMapper.ToRouteName(routePath);
        menu.Title = meta.Title ?? request.Name;
        menu.Icon = meta.Icon;
        menu.ActiveIcon = meta.ActiveIcon;
        menu.ActivePath = meta.ActivePath;
        menu.MenuType = menuType;
        menu.SortOrder = meta.Order ?? 0;
        menu.Visible = meta.HideInMenu != true;
        menu.AffixTab = meta.AffixTab == true;
        menu.AffixTabOrder = meta.AffixTabOrder;
        menu.HideInTab = meta.HideInTab == true;
        menu.KeepAlive = meta.KeepAlive == true;
        menu.HideChildrenInMenu = meta.HideChildrenInMenu == true;
        menu.Link = meta.Link;
        menu.IframeSrc = meta.IframeSrc;
        menu.OpenInNewWindow = meta.OpenInNewWindow == true;
        menu.Enabled = request.Status == 1;
        menu.MetaJson = menuType == "button" ? null : MenuMapper.BuildExtensionMetaJson(meta);
        menu.UpdatedAt = now;

        var menuRepository = systemContext.GetMenuRepository();
        if (id.HasValue)
        {
            await menuRepository.UpdateAsync(menu, cancellationToken);
        }
        else
        {
            await menuRepository.InsertAsync(menu, cancellationToken);
        }

        await context.BumpSessionVersionAsync(cancellationToken);
        menu = await repository.GetMenuWithFeatureAsync(menu.Id, cancellationToken)
               ?? throw new NotFoundDomainException("菜单不存在。");
        var features = ExtractFeatures([menu]);
        return MenuMapper.ToSystemMenuDto(menu, features.ToDictionary(item => item.FeatureCode, StringComparer.OrdinalIgnoreCase), []);
    }

    /// <summary>
    /// 调整同级菜单排序。
    /// </summary>
    public async Task ReorderMenusAsync(ReorderSystemMenuRequest request, CancellationToken cancellationToken)
    {
        var parentId = AdminIdHelper.ParseNullableId(request.Pid);
        var orderedIds = request.OrderedIds!.Select(AdminIdHelper.ParseId).ToArray();
        var siblings = await repository.ListSiblingMenusAsync(parentId, cancellationToken);
        if (siblings.Count != orderedIds.Length)
        {
            throw new ConflictDomainException("排序列表与当前同级菜单数量不一致，请刷新后重试。");
        }

        var siblingIds = siblings.Select(menu => menu.Id).ToHashSet();
        if (orderedIds.Any(item => !siblingIds.Contains(item)))
        {
            throw new ValidationDomainException("只能在当前父级下调整同级菜单顺序。");
        }

        var now = DateTimeOffset.UtcNow;
        var menuMap = siblings.ToDictionary(menu => menu.Id);
        var changed = false;
        for (var index = 0; index < orderedIds.Length; index++)
        {
            var menu = menuMap[orderedIds[index]];
            var order = (index + 1) * 10;
            if (menu.SortOrder != order)
            {
                menu.SortOrder = order;
                menu.UpdatedAt = now;
                changed = true;
            }
        }

        if (!changed)
        {
            return;
        }

        await repository.UpdateMenusAsync(siblings, cancellationToken);
        await context.BumpSessionVersionAsync(cancellationToken);
    }

    /// <summary>
    /// 删除菜单及其角色关联。
    /// </summary>
    public async Task DeleteMenuAsync(long id, CancellationToken cancellationToken)
    {
        if (await repository.CountChildMenusAsync(id, cancellationToken) > 0)
        {
            throw new ConflictDomainException("请先删除下级菜单。");
        }

        _ = await repository.GetMenuWithFeatureAsync(id, cancellationToken)
            ?? throw new NotFoundDomainException("菜单不存在。");
        await systemContext.GetMenuRepository().DeleteCascadeByDatabaseAsync(menu => menu.Id == id, cancellationToken);
        await context.BumpSessionVersionAsync(cancellationToken);
    }

    /// <summary>
    /// 判断菜单名称是否已存在。
    /// </summary>
    public Task<bool> MenuNameExistsAsync(string name, long? id, CancellationToken cancellationToken) =>
        repository.MenuNameExistsAsync(name, id, cancellationToken);

    /// <summary>
    /// 判断菜单路径是否已存在。
    /// </summary>
    public Task<bool> MenuPathExistsAsync(string path, long? id, CancellationToken cancellationToken) =>
        repository.MenuPathExistsAsync(path, id, cancellationToken);

    /// <summary>
    /// 从菜单集合中提取去重后的绑定功能。
    /// </summary>
    private static IReadOnlyList<AdminFeature> ExtractFeatures(IReadOnlyList<AdminMenu> menus) =>
        menus
            .Where(menu => menu.AdminFeature is not null)
            .Select(menu => menu.AdminFeature!)
            .GroupBy(feature => feature.Id)
            .Select(group => group.First())
            .ToArray();

    /// <summary>
    /// 校验菜单父级存在且不会形成循环。
    /// </summary>
    private async Task EnsureValidParentAsync(long? currentId, long? parentId, CancellationToken cancellationToken)
    {
        if (!parentId.HasValue)
        {
            return;
        }

        if (currentId == parentId)
        {
            throw new ValidationDomainException("上级菜单不能选择当前菜单。");
        }

        var menus = await repository.ListMenusWithFeaturesAsync(cancellationToken);
        if (menus.All(menu => menu.Id != parentId.Value))
        {
            throw new NotFoundDomainException("上级菜单不存在。");
        }

        if (!currentId.HasValue)
        {
            return;
        }

        var nextParentId = parentId;
        while (nextParentId.HasValue)
        {
            if (nextParentId.Value == currentId.Value)
            {
                throw new ValidationDomainException("上级菜单不能选择当前菜单的下级菜单。");
            }

            nextParentId = menus.FirstOrDefault(menu => menu.Id == nextParentId.Value)?.ParentId;
        }
    }
}
