using HarborAdmin.BuildingBlocks.Abstractions.Application;
using HarborAdmin.BuildingBlocks.Abstractions.Enums;
using HarborAdmin.BuildingBlocks.Abstractions.Exception;
using HarborAdmin.BuildingBlocks.Abstractions.ModelResults;
using HarborAdmin.BuildingBlocks.Mapping;
using HarborAdmin.Modules.Admin.Application.Abstractions;
using HarborAdmin.Modules.Admin.Application.Services.Shared;
using HarborAdmin.Modules.Admin.Contracts.System.Dto;
using HarborAdmin.Modules.Admin.Contracts.System.Request;
using HarborAdmin.Modules.Admin.Domain.Entities;

namespace HarborAdmin.Modules.Admin.Application.Services.Role;

/// <summary>
/// 角色管理服务。
/// </summary>
public sealed class RoleService(
    AdminServiceContext context,
    IAdminRoleRepository roleRepository,
    IAdminMenuRepository menuRepository,
    IHarborMapper mapper)
    : HarborApplicationPagedRepositoryService<AdminRole, SystemRoleDto, PageRequest, SaveSystemRoleRequest, IAdminRoleRepository>(roleRepository)
{
    /// <inheritdoc />
    protected override SystemRoleDto MapToDto(AdminRole entity) => mapper.Map<SystemRoleDto>(entity);

    /// <inheritdoc />
    protected override AdminRole CreateEntity(SaveSystemRoleRequest request) => new() { CreatedAt = UtcNow };

    /// <summary>
    /// 将保存请求应用到角色聚合。
    /// </summary>
    protected override async Task ApplySaveAsync(AdminRole role, SaveSystemRoleRequest request, CancellationToken cancellationToken)
    {
        role.Name = request.Name;
        role.RoleCode = string.IsNullOrWhiteSpace(request.RoleCode) ? AdminIdHelper.BuildCode(request.Name) : request.RoleCode;
        role.DataScopeType = string.IsNullOrWhiteSpace(request.DataScopeType) ? "Self" : request.DataScopeType;
        role.Remark = request.Remark;
        role.Enabled = request.Status == 1;
        role.UpdatedAt = UtcNow;
        var menuIds = (request.MenuIds ?? ExtractMenuIds(request.Permissions ?? []))
            .Select(AdminIdHelper.ParseId)
            .Distinct()
            .ToArray();
        await EnsureMenusExistAsync(menuIds, cancellationToken);
        role.RoleMenus = menuIds
            .Select(menuId => new AdminRoleMenu { RoleId = role.Id, MenuId = menuId })
            .ToList();

        var permissionCodes = (request.PermissionCodes ?? ExtractPermissionCodes(request.Permissions ?? []))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var rolePermissions = new List<AdminRolePermission>(permissionCodes.Length);
        foreach (var permissionCode in permissionCodes)
        {
            var action = await Repository.GetFeatureActionByPermissionCodeAsync(permissionCode, cancellationToken);
            rolePermissions.Add(new AdminRolePermission
            {
                RoleId = role.Id,
                AdminFeatureActionId = action.Id,
                PermissionCode = action.PermissionCode,
            });
        }

        role.RolePermissions = rolePermissions;
        role.FieldPermissions = await BuildFieldPermissionsAsync(role.Id, request.FieldPolicies ?? [], cancellationToken);
        role.DataScopes =
        [
            new AdminRoleDataScope
            {
                RoleId = role.Id,
                ScopeType = role.DataScopeType,
            },
        ];
    }

    /// <inheritdoc />
    protected override async Task AfterSaveAsync(AdminRole entity, SaveSystemRoleRequest request, CancellationToken cancellationToken) =>
        await context.BumpSessionVersionAsync(cancellationToken);

    /// <inheritdoc />
    protected override async Task AfterDeleteAsync(AdminRole entity, CrudDeleteDecision decision, CancellationToken cancellationToken) =>
        await context.BumpSessionVersionAsync(cancellationToken);

    /// <inheritdoc />
    protected override string GetNotFoundMessage(long id) => "角色不存在。";

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
            var field = await Repository.GetFeatureFieldAsync(policy.FeatureCode, policy.FieldName, cancellationToken);
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

        var menus = await menuRepository.GetMenusByIdsAsync(menuIds, cancellationToken);
        var existingIds = menus.Select(menu => menu.Id).ToHashSet();
        var missingIds = menuIds.Where(menuId => !existingIds.Contains(menuId)).ToArray();
        if (missingIds.Length > 0)
        {
            throw new NotFoundDomainException($"菜单不存在：{string.Join(", ", missingIds)}");
        }
    }
}
