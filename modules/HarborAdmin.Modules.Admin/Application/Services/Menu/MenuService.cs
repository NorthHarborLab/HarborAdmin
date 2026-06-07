using System.Text.Json;
using HarborAdmin.BuildingBlocks.Abstractions.Exception;
using HarborAdmin.Modules.Admin.Contracts.System;
using HarborAdmin.Modules.Admin.Domain.Entities;
using HarborAdmin.Modules.Admin.Application.Services.Shared;

namespace HarborAdmin.Modules.Admin.Application.Services.Menu;

/// <summary>
/// 菜单管理服务。
/// </summary>
public sealed class MenuService(AdminServiceContext context)
{
    private IFreeSql Orm => context.Orm;

    /// <summary>
    /// 获取完整菜单树。
    /// </summary>
    public async Task<IReadOnlyList<SystemMenuDto>> ListMenusAsync(CancellationToken cancellationToken)
    {
        var menus = await Orm.Select<AdminMenu>()
            .OrderBy(menu => menu.SortOrder)
            .ToListAsync(cancellationToken);
        var features = await Orm.Select<AdminFeature>().ToListAsync(cancellationToken);
        return MenuMapper.BuildSystemMenuTree(menus, features);
    }

    /// <summary>
    /// 获取包含按钮权限节点的菜单权限树。
    /// </summary>
    public async Task<IReadOnlyList<SystemMenuDto>> ListMenuPermissionTreeAsync(CancellationToken cancellationToken)
    {
        var menus = await Orm.Select<AdminMenu>()
            .OrderBy(menu => menu.SortOrder)
            .ToListAsync(cancellationToken);
        var features = await Orm.Select<AdminFeature>().ToListAsync(cancellationToken);
        var actions = await Orm.Select<AdminFeatureAction>().Where(action => action.Enabled).ToListAsync(cancellationToken);
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
        var featureCode = await MenuMapper.EnsureFeatureForMenuAsync(Orm, request, menuType, cancellationToken);
        var menu = id.HasValue
            ? await Orm.Select<AdminMenu>().Where(item => item.Id == id).ToOneAsync(cancellationToken)
              ?? throw new NotFoundDomainException("菜单不存在。")
            : new AdminMenu { CreatedAt = now };

        menu.MenuCode = MenuMapper.BuildMenuCode(menuType, routePath, request.AuthCode);
        menu.ParentId = AdminIdHelper.ParseNullableId(request.Pid);
        menu.FeatureCode = featureCode;
        menu.PermissionCode = string.IsNullOrWhiteSpace(request.AuthCode) ? null : request.AuthCode;
        menu.RoutePath = routePath;
        menu.RouteName = MenuMapper.ToRouteName(routePath);
        menu.Title = meta.Title ?? request.Name;
        menu.Icon = meta.Icon;
        menu.MenuType = menuType;
        menu.SortOrder = meta.Order ?? 0;
        menu.Visible = meta.HideInMenu != true;
        menu.Enabled = request.Status == 1;
        menu.MetaJson = JsonSerializer.Serialize(meta, MenuMapper.JsonOptions);
        menu.UpdatedAt = now;

        if (id.HasValue)
        {
            await Orm.Update<AdminMenu>().SetSource(menu).ExecuteAffrowsAsync(cancellationToken);
        }
        else
        {
            await Orm.Insert(menu).ExecuteAffrowsAsync(cancellationToken);
        }

        await context.BumpSessionVersionAsync(cancellationToken);
        var features = await Orm.Select<AdminFeature>().ToListAsync(cancellationToken);
        return MenuMapper.ToSystemMenuDto(menu, features.ToDictionary(feature => feature.FeatureCode, StringComparer.OrdinalIgnoreCase), []);
    }

    /// <summary>
    /// 调整同级菜单排序。
    /// </summary>
    public async Task ReorderMenusAsync(ReorderSystemMenuRequest request, CancellationToken cancellationToken)
    {
        if (request.OrderedIds is null || request.OrderedIds.Count == 0)
        {
            throw new ValidationDomainException("排序菜单不能为空。");
        }

        var parentId = AdminIdHelper.ParseNullableId(request.Pid);
        var orderedIds = request.OrderedIds.Select(AdminIdHelper.ParseId).ToArray();
        if (orderedIds.Distinct().Count() != orderedIds.Length)
        {
            throw new ValidationDomainException("排序菜单不能重复。");
        }

        var siblingQuery = Orm.Select<AdminMenu>();
        siblingQuery = parentId.HasValue
            ? siblingQuery.Where(menu => menu.ParentId == parentId.Value)
            : siblingQuery.Where(menu => !menu.ParentId.HasValue);
        var siblings = await siblingQuery.OrderBy(menu => menu.SortOrder).ToListAsync(cancellationToken);
        if (siblings.Count != orderedIds.Length)
        {
            throw new ConflictDomainException("排序列表与当前同级菜单数量不一致，请刷新后重试。");
        }

        var siblingIds = siblings.Select(menu => menu.Id).ToHashSet();
        if (orderedIds.Any(id => !siblingIds.Contains(id)))
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
            var meta = MenuMapper.ParseMenuMeta(menu);
            var nextMeta = meta with { Order = order };
            var nextMetaJson = JsonSerializer.Serialize(nextMeta, MenuMapper.JsonOptions);
            if (menu.SortOrder != order || menu.MetaJson != nextMetaJson)
            {
                menu.SortOrder = order;
                menu.MetaJson = nextMetaJson;
                menu.UpdatedAt = now;
                changed = true;
            }
        }

        if (!changed)
        {
            return;
        }

        await Orm.Update<AdminMenu>().SetSource(siblings).ExecuteAffrowsAsync(cancellationToken);
        await context.BumpSessionVersionAsync(cancellationToken);
    }

    /// <summary>
    /// 删除菜单及其角色关联。
    /// </summary>
    public async Task DeleteMenuAsync(long id, CancellationToken cancellationToken)
    {
        var children = await Orm.Select<AdminMenu>().Where(menu => menu.ParentId == id).CountAsync(cancellationToken);
        if (children > 0)
        {
            throw new ConflictDomainException("请先删除下级菜单。");
        }

        await Orm.Delete<AdminRoleMenu>().Where(link => link.MenuId == id).ExecuteAffrowsAsync(cancellationToken);
        var affected = await Orm.Delete<AdminMenu>().Where(menu => menu.Id == id).ExecuteAffrowsAsync(cancellationToken);
        if (affected == 0)
        {
            throw new NotFoundDomainException("菜单不存在。");
        }

        await context.BumpSessionVersionAsync(cancellationToken);
    }

    /// <summary>
    /// 判断菜单名称是否已存在。
    /// </summary>
    public Task<bool> MenuNameExistsAsync(string name, long? id, CancellationToken cancellationToken) =>
        Orm.Select<AdminMenu>()
            .Where(menu => menu.Title == name && (!id.HasValue || menu.Id != id.Value))
            .AnyAsync(cancellationToken);

    /// <summary>
    /// 判断菜单路径是否已存在。
    /// </summary>
    public Task<bool> MenuPathExistsAsync(string path, long? id, CancellationToken cancellationToken) =>
        Orm.Select<AdminMenu>()
            .Where(menu => menu.RoutePath == path && (!id.HasValue || menu.Id != id.Value))
            .AnyAsync(cancellationToken);
}
