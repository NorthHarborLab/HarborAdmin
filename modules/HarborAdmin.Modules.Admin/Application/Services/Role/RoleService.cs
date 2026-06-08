using HarborAdmin.BuildingBlocks.Abstractions.Exception;
using HarborAdmin.BuildingBlocks.Mapping;
using HarborAdmin.Modules.Admin.Application.Abstractions;
using HarborAdmin.Modules.Admin.Contracts.System.Dto;
using HarborAdmin.Modules.Admin.Contracts.System.Request;
using HarborAdmin.Modules.Admin.Domain.Entities;
using HarborAdmin.Modules.Admin.Application.Services.Shared;

namespace HarborAdmin.Modules.Admin.Application.Services.Role;

/// <summary>
/// 角色管理服务。
/// </summary>
public sealed class RoleService(
    SystemServiceContext systemContext,
    AdminServiceContext context,
    IAdminRepository repository,
    IHarborMapper mapper)
{
    /// <summary>
    /// 获取角色列表及其权限配置。
    /// </summary>
    public async Task<IReadOnlyList<SystemRoleDto>> ListRolesAsync(CancellationToken cancellationToken)
    {
        var roles = await repository.ListRolesWithGrantsAsync(cancellationToken);
        return roles.Select(role => mapper.Map<SystemRoleDto>(role)).ToArray();
    }

    /// <summary>
    /// 新增或更新角色，并同步菜单、权限码、字段策略与数据范围。
    /// </summary>
    public async Task<SystemRoleDto> SaveRoleAsync(long? id, SaveSystemRoleRequest request, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        AdminRole role;
        if (id.HasValue)
        {
            role = await systemContext.LoadRoleAggregateAsync(id.Value, cancellationToken);
        }
        else
        {
            role = new AdminRole { CreatedAt = now };
        }

        role.Name = request.Name;
        role.RoleCode = string.IsNullOrWhiteSpace(request.RoleCode) ? AdminIdHelper.BuildCode(request.Name) : request.RoleCode;
        role.DataScopeType = string.IsNullOrWhiteSpace(request.DataScopeType) ? "Self" : request.DataScopeType;
        role.Remark = request.Remark;
        role.Enabled = request.Status == 1;
        role.UpdatedAt = now;

        var roleRepository = systemContext.GetRoleRepository();
        if (id.HasValue)
        {
            await roleRepository.UpdateAsync(role, cancellationToken);
        }
        else
        {
            await roleRepository.InsertAsync(role, cancellationToken);
        }

        var menuIds = (request.MenuIds ?? ExtractMenuIds(request.Permissions ?? []))
            .Select(AdminIdHelper.ParseId)
            .Distinct()
            .ToArray();
        await EnsureMenusExistAsync(menuIds, cancellationToken);
        role.RoleMenus = menuIds
            .Select(menuId => new AdminRoleMenu { RoleId = role.Id, MenuId = menuId })
            .ToList();
        systemContext.SaveRoleChildren(role, nameof(AdminRole.RoleMenus));

        var permissionCodes = (request.PermissionCodes ?? ExtractPermissionCodes(request.Permissions ?? []))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var rolePermissions = new List<AdminRolePermission>(permissionCodes.Length);
        foreach (var permissionCode in permissionCodes)
        {
            var action = await systemContext.ResolveFeatureActionByPermissionCodeAsync(permissionCode, cancellationToken);
            rolePermissions.Add(new AdminRolePermission
            {
                RoleId = role.Id,
                AdminFeatureActionId = action.Id,
                PermissionCode = action.PermissionCode,
            });
        }

        role.RolePermissions = rolePermissions;
        systemContext.SaveRoleChildren(role, nameof(AdminRole.RolePermissions));

        role.FieldPermissions = await BuildFieldPermissionsAsync(role.Id, request.FieldPolicies ?? [], cancellationToken);
        systemContext.SaveRoleChildren(role, nameof(AdminRole.FieldPermissions));

        role.DataScopes =
        [
            new AdminRoleDataScope
            {
                RoleId = role.Id,
                ScopeType = role.DataScopeType,
            },
        ];
        systemContext.SaveRoleChildren(role, nameof(AdminRole.DataScopes));

        await context.BumpSessionVersionAsync(cancellationToken);
        role = await systemContext.LoadRoleAggregateAsync(role.Id, cancellationToken);
        return mapper.Map<SystemRoleDto>(role);
    }

    /// <summary>
    /// 删除角色及其关联授权数据。
    /// </summary>
    public async Task DeleteRoleAsync(long id, CancellationToken cancellationToken)
    {
        _ = await systemContext.LoadRoleAggregateAsync(id, cancellationToken);
        await systemContext.GetRoleRepository().DeleteCascadeByDatabaseAsync(role => role.Id == id, cancellationToken);
        await context.BumpSessionVersionAsync(cancellationToken);
    }

    /// <summary>
    /// 根据请求字段策略构建角色字段权限实体。
    /// </summary>
    private async Task<List<AdminRoleFieldPermission>> BuildFieldPermissionsAsync(
        long roleId,
        IReadOnlyList<SystemRoleFieldPolicyDto> policies,
        CancellationToken cancellationToken)
    {
        var normalized = policies
            .Where(policy => !string.IsNullOrWhiteSpace(policy.FeatureCode) && !string.IsNullOrWhiteSpace(policy.FieldName))
            .GroupBy(policy => $"{policy.FeatureCode.Trim()}\u001F{policy.FieldName.Trim()}", StringComparer.OrdinalIgnoreCase)
            // 同一 Feature 字段多次提交时以后者为准，匹配前端编辑表格的覆盖语义。
            .Select(group => group.Last())
            .ToArray();
        var result = new List<AdminRoleFieldPermission>(normalized.Length);
        foreach (var policy in normalized)
        {
            var field = await systemContext.ResolveFeatureFieldAsync(policy.FeatureCode, policy.FieldName, cancellationToken);
            result.Add(new AdminRoleFieldPermission
            {
                RoleId = roleId,
                AdminFeatureFieldId = field.Id,
                FeatureCode = field.FeatureCode,
                FieldName = field.FieldCode,
                Visible = policy.Visible,
                Editable = policy.Editable,
                Exportable = policy.Exportable,
                Masked = policy.Masked,
            });
        }

        return result;
    }

    /// <summary>
    /// 从前端权限树选中值中提取菜单 ID。
    /// </summary>
    private static IReadOnlyList<string> ExtractMenuIds(IReadOnlyList<string> selectedValues) =>
        selectedValues
            .Where(value => !value.StartsWith("perm:", StringComparison.OrdinalIgnoreCase))
            .ToArray();

    /// <summary>
    /// 从前端权限树选中值中提取权限码。
    /// </summary>
    private static IReadOnlyList<string> ExtractPermissionCodes(IReadOnlyList<string> selectedValues) =>
        selectedValues
            .Where(value => value.StartsWith("perm:", StringComparison.OrdinalIgnoreCase))
            .Select(value => value["perm:".Length..])
            .ToArray();

    /// <summary>
    /// 校验角色绑定的菜单全部存在。
    /// </summary>
    private async Task EnsureMenusExistAsync(IReadOnlyList<long> menuIds, CancellationToken cancellationToken)
    {
        if (menuIds.Count == 0)
        {
            return;
        }

        var menus = await repository.GetMenusByIdsAsync(menuIds, cancellationToken);
        var existingIds = menus.Select(menu => menu.Id).ToHashSet();
        var missingIds = menuIds.Where(menuId => !existingIds.Contains(menuId)).ToArray();
        if (missingIds.Length > 0)
        {
            throw new NotFoundDomainException($"菜单不存在：{string.Join(", ", missingIds)}");
        }
    }
}
