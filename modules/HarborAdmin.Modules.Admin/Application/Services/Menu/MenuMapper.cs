using System.Text.Json;
using HarborAdmin.BuildingBlocks.Abstractions.Exception;
using HarborAdmin.Modules.Admin.Contracts.Access.Dto;
using HarborAdmin.Modules.Admin.Contracts.System.Dto;
using HarborAdmin.Modules.Admin.Contracts.System.Request;
using HarborAdmin.Modules.Admin.Domain.Entities;
using HarborAdmin.Modules.Admin.Application.Services.Shared;

namespace HarborAdmin.Modules.Admin.Application.Services.Menu;

/// <summary>
/// 菜单实体与 DTO 映射工具。
/// </summary>
internal static class MenuMapper
{
    internal static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private static readonly string[] FieldizedMetaKeys =
    [
        "title",
        "icon",
        "activeIcon",
        "activePath",
        "order",
        "affixTab",
        "affixTabOrder",
        "hideInMenu",
        "hideInTab",
        "keepAlive",
        "hideChildrenInMenu",
        "link",
        "iframeSrc",
        "openInNewWindow",
        "featureCode"
    ];

    private static readonly string[] ExtensionMetaKeys =
    [
        "hideInBreadcrumb",
        "badge",
        "badgeType",
        "badgeVariants",
        "maxNumOfOpenTab",
        "noBasicLayout",
        "query"
    ];

    private static readonly HashSet<string> FieldizedMetaKeySet = new(FieldizedMetaKeys, StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> ExtensionMetaKeySet = new(ExtensionMetaKeys, StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> BadgeTypes = new(["dot", "normal"], StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> BadgeVariants = new(["default", "destructive", "primary", "success", "warning"], StringComparer.OrdinalIgnoreCase);

    internal static IReadOnlyList<SystemMenuDto> BuildSystemMenuTree(IReadOnlyList<AdminMenu> menus, IReadOnlyList<AdminFeature> features,
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

    internal static SystemMenuDto ToSystemMenuDto(AdminMenu menu, IReadOnlyDictionary<string, AdminFeature> featureMap, IReadOnlyList<AdminMenu> allMenus,
        IReadOnlyList<AdminFeatureAction>? actions = null)
    {
        var feature = menu.AdminFeature;
        if (feature is null && !featureMap.TryGetValue(menu.FeatureCode ?? string.Empty, out feature))
        {
            feature = null;
        }

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
                    action.LabelKey,
                    null,
                    null,
                    null,
                    0,
                    false,
                    null,
                    false,
                    false,
                    false,
                    false,
                    null,
                    null,
                    false,
                    null,
                    new SystemMenuMetaDto(action.LabelKey, FeatureCode: menu.FeatureCode))))
            .ToArray();
        var meta = ToOutputMenuMeta(menu);
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
            meta.Title,
            meta.Icon,
            meta.ActiveIcon,
            meta.ActivePath,
            meta.Order ?? 0,
            meta.AffixTab == true,
            meta.AffixTabOrder,
            meta.HideInMenu == true,
            meta.HideInTab == true,
            meta.KeepAlive == true,
            meta.HideChildrenInMenu == true,
            meta.Link,
            meta.IframeSrc,
            meta.OpenInNewWindow == true,
            menu.MenuType == "button" ? null : BuildExtensionMetaJson(meta),
            meta,
            null,
            children.Length > 0 ? children : null);
    }

    internal static async Task<IReadOnlyList<BackendRouteDto>> BuildRoutesAsync(IFreeSql orm, IReadOnlyList<AdminMenu> menus, IReadOnlyList<string> permissions,
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

    internal static BackendRouteDto ToBackendRoute(AdminMenu menu, IReadOnlyList<AdminMenu> allMenus, IReadOnlyDictionary<string, AdminFeature> featureMap,
        IReadOnlyList<string> permissions)
    {
        var feature = menu.AdminFeature;
        if (feature is null && !featureMap.TryGetValue(menu.FeatureCode ?? string.Empty, out feature))
        {
            feature = null;
        }

        var children = allMenus
            .Where(child => child.ParentId == menu.Id)
            .OrderBy(child => child.SortOrder)
            .Select(child => ToBackendRoute(child, allMenus, featureMap, permissions))
            .ToArray();
        var meta = ToOutputMenuMeta(menu);
        var routeMeta = new BackendRouteMetaDto(
            meta.Title ?? menu.Title,
            meta.Icon ?? menu.Icon,
            meta.ActiveIcon,
            meta.ActivePath,
            meta.Order,
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

    private static SystemMenuMetaDto ToOutputMenuMeta(AdminMenu menu)
    {
        var meta = ParseMenuMeta(menu);
        return new SystemMenuMetaDto(
            string.IsNullOrWhiteSpace(menu.Title) ? meta.Title : menu.Title,
            menu.Icon ?? meta.Icon,
            menu.ActiveIcon ?? meta.ActiveIcon,
            menu.ActivePath ?? meta.ActivePath,
            menu.SortOrder,
            menu.AffixTab || meta.AffixTab == true,
            menu.AffixTabOrder ?? meta.AffixTabOrder,
            !menu.Visible || meta.HideInMenu == true,
            menu.HideInTab || meta.HideInTab == true,
            meta.HideInBreadcrumb,
            menu.KeepAlive || meta.KeepAlive == true,
            menu.HideChildrenInMenu || meta.HideChildrenInMenu == true,
            meta.Badge,
            meta.BadgeType,
            meta.BadgeVariants,
            menu.Link ?? meta.Link,
            menu.IframeSrc ?? meta.IframeSrc,
            meta.MaxNumOfOpenTab,
            meta.NoBasicLayout,
            menu.OpenInNewWindow || meta.OpenInNewWindow == true,
            meta.Query,
            menu.FeatureCode ?? meta.FeatureCode);
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
        var legacyMeta = request.Meta ?? new SystemMenuMetaDto(request.Name);
        var extensionMeta = ParseExtensionMeta(request.MetaJson, legacyMeta);
        var title = FirstNotEmpty(request.Title, legacyMeta.Title, request.Name) ?? request.Name;
        var activePath = FirstNotEmpty(request.ActivePath, legacyMeta.ActivePath);
        var link = FirstNotEmpty(request.Link, legacyMeta.Link);
        var iframeSrc = FirstNotEmpty(request.IframeSrc, legacyMeta.IframeSrc);

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

        return new SystemMenuMetaDto(
            title,
            FirstNotEmpty(request.Icon, legacyMeta.Icon),
            FirstNotEmpty(request.ActiveIcon, legacyMeta.ActiveIcon),
            NormalizeOptional(activePath),
            request.Order ?? legacyMeta.Order ?? 0,
            request.AffixTab || legacyMeta.AffixTab == true,
            request.AffixTabOrder ?? legacyMeta.AffixTabOrder,
            request.HideInMenu || legacyMeta.HideInMenu == true,
            request.HideInTab || legacyMeta.HideInTab == true,
            extensionMeta.HideInBreadcrumb,
            request.KeepAlive || legacyMeta.KeepAlive == true,
            request.HideChildrenInMenu || legacyMeta.HideChildrenInMenu == true,
            extensionMeta.Badge,
            extensionMeta.BadgeType,
            extensionMeta.BadgeVariants,
            NormalizeOptional(link),
            NormalizeOptional(iframeSrc),
            extensionMeta.MaxNumOfOpenTab,
            extensionMeta.NoBasicLayout,
            request.OpenInNewWindow || legacyMeta.OpenInNewWindow == true,
            extensionMeta.Query,
            FirstNotEmpty(request.FeatureCode, legacyMeta.FeatureCode));
    }

    internal static string BuildExtensionMetaJson(SystemMenuMetaDto meta)
    {
        var extension = new Dictionary<string, object?>
        {
            ["hideInBreadcrumb"] = meta.HideInBreadcrumb ?? false,
            ["badge"] = meta.Badge ?? string.Empty,
            ["badgeType"] = meta.BadgeType,
            ["badgeVariants"] = meta.BadgeVariants,
            ["maxNumOfOpenTab"] = meta.MaxNumOfOpenTab,
            ["noBasicLayout"] = meta.NoBasicLayout ?? false,
            ["query"] = meta.Query ?? new Dictionary<string, object?>()
        };
        return JsonSerializer.Serialize(extension, JsonOptions);
    }

    internal static string NormalizeMenuPath(SaveSystemMenuRequest request, SystemMenuMetaDto meta, string menuType)
    {
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

        var path = request.Path?.Trim();
        if (!string.IsNullOrWhiteSpace(path))
        {
            return path;
        }

        throw new ValidationDomainException("目录、菜单和内嵌菜单必须指定路径。");
    }

    internal static async Task<string?> EnsureFeatureForMenuAsync(IFreeSql orm, SaveSystemMenuRequest request, string menuType, CancellationToken cancellationToken)
    {
        if (menuType is not ("menu" or "embedded" or "link"))
        {
            return null;
        }

        var featureCode = FirstNotEmpty(request.FeatureCode, request.Meta?.FeatureCode);
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

    private static SystemMenuMetaDto ParseExtensionMeta(string? metaJson, SystemMenuMetaDto legacyMeta)
    {
        if (string.IsNullOrWhiteSpace(metaJson))
        {
            return new SystemMenuMetaDto(
                legacyMeta.Title,
                HideInBreadcrumb: legacyMeta.HideInBreadcrumb ?? false,
                Badge: legacyMeta.Badge ?? string.Empty,
                BadgeType: legacyMeta.BadgeType,
                BadgeVariants: legacyMeta.BadgeVariants,
                MaxNumOfOpenTab: legacyMeta.MaxNumOfOpenTab,
                NoBasicLayout: legacyMeta.NoBasicLayout ?? false,
                Query: legacyMeta.Query ?? new Dictionary<string, object?>());
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(metaJson);
        }
        catch (JsonException exception)
        {
            throw new ValidationDomainException($"菜单扩展 MetaJson 不是合法 JSON：{exception.Message}");
        }

        using (document)
        {
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                throw new ValidationDomainException("菜单扩展 MetaJson 必须是 JSON 对象。");
            }

            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (FieldizedMetaKeySet.Contains(property.Name))
                {
                    throw new ValidationDomainException($"菜单扩展 MetaJson 不能包含字段化配置：{property.Name}");
                }

                if (!ExtensionMetaKeySet.Contains(property.Name))
                {
                    throw new ValidationDomainException($"菜单扩展 MetaJson 不支持字段：{property.Name}");
                }

                if (property.NameEquals("query") && property.Value.ValueKind is not JsonValueKind.Object and not JsonValueKind.Null)
                {
                    throw new ValidationDomainException("菜单扩展 MetaJson 的 query 必须是 JSON 对象。");
                }

                if (property.NameEquals("badgeType"))
                {
                    EnsureAllowedStringOrNull(property, BadgeTypes, "badgeType");
                }

                if (property.NameEquals("badgeVariants"))
                {
                    EnsureAllowedStringOrNull(property, BadgeVariants, "badgeVariants");
                }
            }
        }

        return JsonSerializer.Deserialize<SystemMenuMetaDto>(metaJson, JsonOptions)
               ?? new SystemMenuMetaDto(legacyMeta.Title);
    }

    private static void EnsureAllowedStringOrNull(JsonProperty property, IReadOnlySet<string> allowedValues, string fieldName)
    {
        if (property.Value.ValueKind == JsonValueKind.Null)
        {
            return;
        }

        if (property.Value.ValueKind != JsonValueKind.String)
        {
            throw new ValidationDomainException($"菜单扩展 MetaJson 的 {fieldName} 必须是字符串或 null。");
        }

        var value = property.Value.GetString();
        if (string.IsNullOrWhiteSpace(value) || !allowedValues.Contains(value))
        {
            throw new ValidationDomainException($"菜单扩展 MetaJson 的 {fieldName} 值不合法：{value}");
        }
    }

    private static string? FirstNotEmpty(params string?[] values) =>
        values.Select(NormalizeOptional).FirstOrDefault(value => value is not null);

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}