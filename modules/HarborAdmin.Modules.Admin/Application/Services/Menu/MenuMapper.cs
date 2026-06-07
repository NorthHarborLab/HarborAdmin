using System.Text.Json;
using HarborAdmin.BuildingBlocks.Abstractions.Exception;
using HarborAdmin.Modules.Admin.Contracts.Auth.Dto;
using HarborAdmin.Modules.Admin.Contracts.System;
using HarborAdmin.Modules.Admin.Domain.Entities;
using HarborAdmin.Modules.Admin.Application.Services.Shared;

namespace HarborAdmin.Modules.Admin.Application.Services.Menu;

/// <summary>
/// 菜单实体与 DTO 映射工具。
/// </summary>
internal static class MenuMapper
{
    internal static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    internal static IReadOnlyList<SystemMenuDto> BuildSystemMenuTree(
        IReadOnlyList<AdminMenu> menus,
        IReadOnlyList<AdminFeature> features,
        IReadOnlyList<AdminFeatureAction>? actions = null)
    {
        var visibleMenus = actions is null
            ? menus
            : menus.Where(menu => menu.MenuType != "button").ToArray();
        var featureMap = features.ToDictionary(feature => feature.FeatureCode, StringComparer.OrdinalIgnoreCase);
        return visibleMenus
            .Where(menu => !menu.ParentId.HasValue)
            .OrderBy(menu => menu.SortOrder)
            .Select(menu => ToSystemMenuDto(menu, featureMap, visibleMenus, actions ?? []))
            .ToArray();
    }

    internal static SystemMenuDto ToSystemMenuDto(
        AdminMenu menu,
        IReadOnlyDictionary<string, AdminFeature> featureMap,
        IReadOnlyList<AdminMenu> allMenus,
        IReadOnlyList<AdminFeatureAction>? actions = null)
    {
        featureMap.TryGetValue(menu.FeatureCode ?? string.Empty, out var feature);
        var children = allMenus
            .Where(child => child.ParentId == menu.Id)
            .OrderBy(child => child.SortOrder)
            .Select(child => ToSystemMenuDto(child, featureMap, allMenus, actions ?? []))
            .Concat((actions ?? [])
                .Where(action => !string.IsNullOrWhiteSpace(menu.FeatureCode)
                                 && action.FeatureCode == menu.FeatureCode)
                .Select(action => new SystemMenuDto(
                    $"perm:{action.PermissionCode}",
                    menu.Id.ToString(),
                    action.LabelFallback ?? action.ActionCode,
                    $"{menu.RoutePath}#{action.PermissionCode}",
                    "button",
                    menu.FeatureCode,
                    null,
                    action.PermissionCode,
                    1,
                    new SystemMenuMetaDto(action.LabelKey, FeatureCode: menu.FeatureCode),
                    null,
                    null)))
            .ToArray();
        var meta = ParseMenuMeta(menu);
        return new SystemMenuDto(
            menu.Id.ToString(),
            menu.ParentId?.ToString() ?? "0",
            menu.Title,
            menu.RoutePath,
            menu.MenuType,
            menu.FeatureCode,
            feature?.Component,
            menu.PermissionCode,
            menu.Enabled ? 1 : 0,
            meta,
            null,
            children.Length > 0 ? children : null);
    }

    internal static async Task<IReadOnlyList<BackendRouteDto>> BuildRoutesAsync(
        IFreeSql orm,
        IReadOnlyList<AdminMenu> menus,
        IReadOnlyList<string> permissions,
        CancellationToken cancellationToken)
    {
        var features = await orm.Select<AdminFeature>().Where(feature => feature.Enabled).ToListAsync(cancellationToken);
        var featureMap = features.ToDictionary(feature => feature.FeatureCode, StringComparer.OrdinalIgnoreCase);
        var routeMenus = menus
            .Where(menu => menu.MenuType != "button")
            .ToArray();
        var nodes = routeMenus.Where(menu => !menu.ParentId.HasValue).OrderBy(menu => menu.SortOrder).ToArray();
        return nodes.Select(menu => ToBackendRoute(menu, routeMenus, featureMap, permissions)).ToArray();
    }

    internal static BackendRouteDto ToBackendRoute(
        AdminMenu menu,
        IReadOnlyList<AdminMenu> allMenus,
        IReadOnlyDictionary<string, AdminFeature> featureMap,
        IReadOnlyList<string> permissions)
    {
        featureMap.TryGetValue(menu.FeatureCode ?? string.Empty, out var feature);
        var children = allMenus
            .Where(child => child.ParentId == menu.Id)
            .OrderBy(child => child.SortOrder)
            .Select(child => ToBackendRoute(child, allMenus, featureMap, permissions))
            .ToArray();
        var meta = ParseMenuMeta(menu);
        var routeMeta = new BackendRouteMetaDto(
            meta.Title ?? menu.Title,
            meta.Icon ?? menu.Icon,
            meta.ActiveIcon,
            meta.ActivePath,
            meta.Order ?? menu.SortOrder,
            meta.AffixTab,
            meta.AffixTabOrder,
            meta.HideInMenu ?? !menu.Visible,
            meta.HideInTab,
            meta.HideInBreadcrumb,
            meta.KeepAlive,
            meta.HideChildrenInMenu,
            meta.Badge,
            meta.BadgeType,
            meta.BadgeVariants,
            meta.Link,
            meta.IframeSrc,
            meta.MaxNumOfOpenTab,
            meta.NoBasicLayout,
            meta.OpenInNewWindow,
            meta.Query,
            menu.FeatureCode ?? meta.FeatureCode);
        var component = menu.MenuType switch
        {
            "catalog" => "BasicLayout",
            "embedded" => feature?.Component ?? "IFrameView",
            "link" => feature?.Component ?? "BasicLayout",
            _ => feature?.Component ?? "BasicLayout",
        };

        return new BackendRouteDto(
            menu.RoutePath,
            menu.RouteName,
            component,
            routeMeta,
            children.Length > 0 ? children[0].Path : null,
            children.Length > 0 ? children : null);
    }

    internal static SystemMenuMetaDto ParseMenuMeta(AdminMenu menu)
    {
        if (!string.IsNullOrWhiteSpace(menu.MetaJson))
        {
            try
            {
                return JsonSerializer.Deserialize<SystemMenuMetaDto>(menu.MetaJson, JsonOptions) ?? new SystemMenuMetaDto(menu.Title);
            }
            catch
            {
                return new SystemMenuMetaDto(menu.Title, menu.Icon, Order: menu.SortOrder);
            }
        }

        return new SystemMenuMetaDto(menu.Title, menu.Icon, Order: menu.SortOrder);
    }

    internal static string NormalizeMenuType(string? type) =>
        string.IsNullOrWhiteSpace(type)
            ? "menu"
            : type.Trim().ToLowerInvariant() switch
            {
                "catalog" => "catalog",
                "menu" => "menu",
                "embedded" => "embedded",
                "link" => "link",
                "button" => "button",
                _ => throw new ValidationDomainException($"不支持的菜单类型：{type}")
            };

    internal static SystemMenuMetaDto NormalizeMenuMeta(SaveSystemMenuRequest request, string menuType)
    {
        var meta = request.Meta ?? new SystemMenuMetaDto(request.Name);
        var title = string.IsNullOrWhiteSpace(meta.Title) ? request.Name : meta.Title;
        var activePath = string.IsNullOrWhiteSpace(meta.ActivePath) ? request.ActivePath : meta.ActivePath;
        var link = meta.Link;
        var iframeSrc = meta.IframeSrc;

        if (!string.IsNullOrWhiteSpace(request.LinkSrc))
        {
            if (menuType == "link")
            {
                link = request.LinkSrc.Trim();
            }
            else if (menuType == "embedded")
            {
                iframeSrc = request.LinkSrc.Trim();
            }
        }

        return meta with
        {
            Title = title,
            ActivePath = string.IsNullOrWhiteSpace(activePath) ? null : activePath,
            Link = string.IsNullOrWhiteSpace(link) ? null : link,
            IframeSrc = string.IsNullOrWhiteSpace(iframeSrc) ? null : iframeSrc,
        };
    }

    internal static string NormalizeMenuPath(SaveSystemMenuRequest request, SystemMenuMetaDto meta, string menuType)
    {
        var path = request.Path?.Trim();
        if (!string.IsNullOrWhiteSpace(path))
        {
            return path;
        }

        if (menuType == "button")
        {
            var authCode = string.IsNullOrWhiteSpace(request.AuthCode) ? request.Name : request.AuthCode;
            return $"#{authCode}";
        }

        if (menuType == "link")
        {
            var source = meta.Link ?? request.Name;
            return $"/external/{AdminIdHelper.BuildCode(source)}";
        }

        throw new ValidationDomainException("目录、菜单和内嵌菜单必须指定路径。");
    }

    internal static async Task<string?> EnsureFeatureForMenuAsync(
        IFreeSql orm,
        SaveSystemMenuRequest request,
        string menuType,
        CancellationToken cancellationToken)
    {
        if (menuType is not ("menu" or "embedded" or "link"))
        {
            return null;
        }

        var featureCode = request.FeatureCode?.Trim();
        if (string.IsNullOrWhiteSpace(featureCode))
        {
            throw new ValidationDomainException("菜单必须选择已存在的功能资源。");
        }

        var exists = await orm.Select<AdminFeature>()
            .Where(feature => feature.FeatureCode == featureCode && feature.Enabled)
            .AnyAsync(cancellationToken);
        if (!exists)
        {
            throw new NotFoundDomainException($"Feature '{featureCode}' was not found.");
        }

        return featureCode;
    }

    internal static string BuildMenuCode(string menuType, string path, string? authCode) =>
        menuType == "button" && !string.IsNullOrWhiteSpace(authCode)
            ? $"button.{AdminIdHelper.BuildCode(authCode)}"
            : $"menu.{path.Trim('/').Replace('/', '.').Replace('-', '_')}";

    internal static string ToRouteName(string path)
    {
        var parts = path.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        return string.Concat(parts.Select(part => char.ToUpperInvariant(part[0]) + part[1..].Replace("-", string.Empty)));
    }
}
