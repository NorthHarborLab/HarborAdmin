using HarborAdmin.BuildingBlocks.Abstractions.Exception;
using HarborAdmin.Modules.Admin.Contracts.System;
using HarborAdmin.Modules.Admin.Domain.Entities;
using HarborAdmin.Modules.Admin.Application.Services.Shared;

namespace HarborAdmin.Modules.Admin.Application.Services.Role;

/// <summary>
/// 角色管理服务。
/// </summary>
public sealed class RoleService(AdminServiceContext context)
{
    private IFreeSql Orm => context.Orm;

    /// <summary>
    /// 获取角色列表及其权限配置。
    /// </summary>
    public async Task<IReadOnlyList<SystemRoleDto>> ListRolesAsync(CancellationToken cancellationToken)
    {
        var roles = await Orm.Select<AdminRole>().OrderBy(role => role.Id).ToListAsync(cancellationToken);
        var links = await Orm.Select<AdminRoleMenu>().ToListAsync(cancellationToken);
        var permissionLinks = await Orm.Select<AdminRolePermission>().ToListAsync(cancellationToken);
        var fieldPolicies = await Orm.Select<AdminRoleFieldPermission>().ToListAsync(cancellationToken);
        return roles.Select(role => ToRoleDto(role, links, permissionLinks, fieldPolicies)).ToArray();
    }

    /// <summary>
    /// 新增或更新角色，并同步菜单、权限码、字段策略与数据范围。
    /// </summary>
    public async Task<SystemRoleDto> SaveRoleAsync(long? id, SaveSystemRoleRequest request, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var role = id.HasValue
            ? await Orm.Select<AdminRole>().Where(item => item.Id == id).ToOneAsync(cancellationToken)
              ?? throw new NotFoundDomainException("角色不存在。")
            : new AdminRole { CreatedAt = now };
        role.Name = request.Name;
        role.RoleCode = string.IsNullOrWhiteSpace(request.RoleCode) ? AdminIdHelper.BuildCode(request.Name) : request.RoleCode;
        role.DataScopeType = string.IsNullOrWhiteSpace(request.DataScopeType) ? "Self" : request.DataScopeType;
        role.Remark = request.Remark;
        role.Enabled = request.Status == 1;
        role.UpdatedAt = now;

        if (id.HasValue)
        {
            await Orm.Update<AdminRole>().SetSource(role).ExecuteAffrowsAsync(cancellationToken);
        }
        else
        {
            await Orm.Insert(role).ExecuteAffrowsAsync(cancellationToken);
        }

        await ReplaceRolePermissionsAsync(
            role.Id,
            request.MenuIds ?? ExtractMenuIds(request.Permissions ?? []),
            request.PermissionCodes ?? ExtractPermissionCodes(request.Permissions ?? []),
            cancellationToken);
        await ReplaceRoleFieldPoliciesAsync(role.Id, request.FieldPolicies ?? [], cancellationToken);
        await ReplaceRoleDataScopeAsync(role, cancellationToken);
        await context.BumpSessionVersionAsync(cancellationToken);
        var roleMenus = await Orm.Select<AdminRoleMenu>().ToListAsync(cancellationToken);
        var rolePermissions = await Orm.Select<AdminRolePermission>().ToListAsync(cancellationToken);
        var roleFieldPolicies = await Orm.Select<AdminRoleFieldPermission>().ToListAsync(cancellationToken);
        return ToRoleDto(role, roleMenus, rolePermissions, roleFieldPolicies);
    }

    /// <summary>
    /// 删除角色及其关联授权数据。
    /// </summary>
    public async Task DeleteRoleAsync(long id, CancellationToken cancellationToken)
    {
        await Orm.Delete<AdminUserRole>().Where(link => link.RoleId == id).ExecuteAffrowsAsync(cancellationToken);
        await Orm.Delete<AdminRoleMenu>().Where(link => link.RoleId == id).ExecuteAffrowsAsync(cancellationToken);
        await Orm.Delete<AdminRolePermission>().Where(link => link.RoleId == id).ExecuteAffrowsAsync(cancellationToken);
        await Orm.Delete<AdminRoleFieldPermission>().Where(link => link.RoleId == id).ExecuteAffrowsAsync(cancellationToken);
        await Orm.Delete<AdminRoleDataScope>().Where(link => link.RoleId == id).ExecuteAffrowsAsync(cancellationToken);
        await Orm.Delete<AdminRole>().Where(role => role.Id == id).ExecuteAffrowsAsync(cancellationToken);
        await context.BumpSessionVersionAsync(cancellationToken);
    }

    private async Task ReplaceRolePermissionsAsync(
        long roleId,
        IReadOnlyList<string> selectedMenuIds,
        IReadOnlyList<string> selectedPermissionCodes,
        CancellationToken cancellationToken)
    {
        await Orm.Delete<AdminRoleMenu>().Where(link => link.RoleId == roleId).ExecuteAffrowsAsync(cancellationToken);
        await Orm.Delete<AdminRolePermission>().Where(link => link.RoleId == roleId).ExecuteAffrowsAsync(cancellationToken);
        var menuIds = selectedMenuIds
            .Select(AdminIdHelper.ParseId)
            .Distinct()
            .ToArray();
        var permissionCodes = selectedPermissionCodes
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (menuIds.Length > 0)
        {
            await Orm.Insert(menuIds.Select(menuId => new AdminRoleMenu { RoleId = roleId, MenuId = menuId })).ExecuteAffrowsAsync(cancellationToken);
        }

        if (permissionCodes.Length > 0)
        {
            await Orm.Insert(permissionCodes.Select(code => new AdminRolePermission { RoleId = roleId, PermissionCode = code }))
                .ExecuteAffrowsAsync(cancellationToken);
        }
    }

    private async Task ReplaceRoleFieldPoliciesAsync(long roleId, IReadOnlyList<SystemRoleFieldPolicyDto> policies, CancellationToken cancellationToken)
    {
        await Orm.Delete<AdminRoleFieldPermission>().Where(link => link.RoleId == roleId).ExecuteAffrowsAsync(cancellationToken);
        var normalized = policies
            .Where(policy => !string.IsNullOrWhiteSpace(policy.FeatureCode) && !string.IsNullOrWhiteSpace(policy.FieldName))
            .GroupBy(policy => $"{policy.FeatureCode.Trim()}\u001F{policy.FieldName.Trim()}", StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var policy = group.Last();
                return new AdminRoleFieldPermission
                {
                    RoleId = roleId,
                    FeatureCode = policy.FeatureCode.Trim(),
                    FieldName = policy.FieldName.Trim(),
                    Visible = policy.Visible,
                    Editable = policy.Editable,
                    Exportable = policy.Exportable,
                    Masked = policy.Masked,
                };
            })
            .ToArray();
        if (normalized.Length > 0)
        {
            await Orm.Insert(normalized).ExecuteAffrowsAsync(cancellationToken);
        }
    }

    private static IReadOnlyList<string> ExtractMenuIds(IReadOnlyList<string> selectedValues) =>
        selectedValues
            .Where(value => !value.StartsWith("perm:", StringComparison.OrdinalIgnoreCase))
            .ToArray();

    private static IReadOnlyList<string> ExtractPermissionCodes(IReadOnlyList<string> selectedValues) =>
        selectedValues
            .Where(value => value.StartsWith("perm:", StringComparison.OrdinalIgnoreCase))
            .Select(value => value["perm:".Length..])
            .ToArray();

    private async Task ReplaceRoleDataScopeAsync(AdminRole role, CancellationToken cancellationToken)
    {
        await Orm.Delete<AdminRoleDataScope>().Where(scope => scope.RoleId == role.Id).ExecuteAffrowsAsync(cancellationToken);
        await Orm.Insert(new AdminRoleDataScope
        {
            RoleId = role.Id,
            ScopeType = role.DataScopeType,
        }).ExecuteAffrowsAsync(cancellationToken);
    }

    private static SystemRoleDto ToRoleDto(
        AdminRole role,
        IReadOnlyList<AdminRoleMenu> menus,
        IReadOnlyList<AdminRolePermission> permissions,
        IReadOnlyList<AdminRoleFieldPermission> fieldPolicies)
    {
        var menuIds = menus
            .Where(link => link.RoleId == role.Id)
            .Select(link => link.MenuId.ToString())
            .ToArray();
        var permissionCodes = permissions
            .Where(link => link.RoleId == role.Id)
            .Select(link => link.PermissionCode)
            .ToArray();
        var values = menuIds.Concat(permissionCodes.Select(code => $"perm:{code}")).ToArray();
        var policies = fieldPolicies
            .Where(policy => policy.RoleId == role.Id)
            .Select(policy => new SystemRoleFieldPolicyDto(
                policy.FeatureCode,
                policy.FieldName,
                policy.Visible,
                policy.Editable,
                policy.Exportable,
                policy.Masked))
            .ToArray();
        return new SystemRoleDto(
            role.Id.ToString(),
            role.Name,
            role.RoleCode,
            menuIds,
            permissionCodes,
            policies,
            values,
            role.Remark,
            role.Enabled ? 1 : 0,
            role.DataScopeType,
            role.CreatedAt.ToString("O"));
    }
}
