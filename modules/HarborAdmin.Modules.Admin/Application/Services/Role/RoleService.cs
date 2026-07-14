using HarborAdmin.BuildingBlocks.Application;
using HarborAdmin.BuildingBlocks.Abstractions.Enums;
using HarborAdmin.BuildingBlocks.Abstractions.Exception;
using HarborAdmin.BuildingBlocks.Abstractions.ModelResults;
using HarborAdmin.BuildingBlocks.Abstractions.Repositories;
using HarborAdmin.BuildingBlocks.Abstractions.Results;
using HarborAdmin.BuildingBlocks.Mapping;
using HarborAdmin.Modules.Admin.Application.Abstractions;
using HarborAdmin.Modules.Admin.Application.Services.Shared;
using HarborAdmin.Modules.Admin.Contracts.System.Dto;
using HarborAdmin.Modules.Admin.Contracts.System.Request;
using HarborAdmin.Modules.Admin.Contracts.Shared.ErrorCode;
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
    : HarborCrudApplicationService<AdminRole, SystemRoleDto, PageRequest, SaveSystemRoleRequest, IAdminRoleRepository>(roleRepository)
{
    /// <inheritdoc />
    protected override SystemRoleDto MapToDto(AdminRole entity) => mapper.Map<SystemRoleDto>(entity);

    /// <inheritdoc />
    protected override AdminRole CreateEntity(SaveSystemRoleRequest request) => new() { CreatedAt = UtcNow };

    /// <summary>
    /// 将保存请求应用到角色聚合。
    /// </summary>
    protected override async Task<HarborResult> ApplySaveAsync(AdminRole role, SaveSystemRoleRequest request, CancellationToken cancellationToken)
    {
        role.Name = request.Name;
        role.RoleCode = string.IsNullOrWhiteSpace(request.RoleCode) ? AdminIdHelper.BuildCode(request.Name) : request.RoleCode.Trim();
        role.DataScopeType = string.IsNullOrWhiteSpace(request.DataScopeType) ? "Self" : request.DataScopeType;
        role.Remark = request.Remark;
        role.Enabled = request.Status == 1;
        role.UpdatedAt = UtcNow;
        if (await Repository.RoleCodeExistsAsync(
                role.RoleCode,
                role.Id > 0 ? role.Id : null,
                cancellationToken))
        {
            return HarborResult.Failure(AdminRoleErrorCodes.DuplicateCode.Create(
                new Dictionary<string, object?> { ["roleCode"] = role.RoleCode }));
        }

        var menuIds = (request.MenuIds ?? ExtractMenuIds(request.Permissions ?? []))
            .Select(AdminIdHelper.ParseId)
            .Distinct()
            .ToArray();
        var menuValidation = await ValidateMenusAsync(menuIds, cancellationToken);
        if (!menuValidation.IsSuccess)
        {
            return menuValidation;
        }

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
            AdminFeatureAction action;
            try
            {
                action = await Repository.GetFeatureActionByPermissionCodeAsync(permissionCode, cancellationToken);
            }
            catch (NotFoundDomainException)
            {
                return HarborResult.Failure(AdminRoleErrorCodes.PermissionNotFound.Create(
                    new Dictionary<string, object?> { ["permissionCode"] = permissionCode }));
            }

            rolePermissions.Add(new AdminRolePermission
            {
                RoleId = role.Id,
                AdminFeatureActionId = action.Id,
                PermissionCode = action.PermissionCode,
            });
        }

        role.RolePermissions = rolePermissions;
        try
        {
            role.FieldPermissions = await BuildFieldPermissionsAsync(role.Id, request.FieldPolicies ?? [], cancellationToken);
        }
        catch (NotFoundDomainException exception)
        {
            return HarborResult.Failure(AdminRoleErrorCodes.PermissionNotFound.Create(
                new Dictionary<string, object?> { ["permissionCode"] = exception.Message }));
        }

        role.DataScopes =
        [
            new AdminRoleDataScope
            {
                RoleId = role.Id,
                ScopeType = role.DataScopeType,
            },
        ];
        return HarborResult.Success();
    }

    /// <inheritdoc />
    protected override async Task AfterSaveAsync(AdminRole entity, SaveSystemRoleRequest request, CancellationToken cancellationToken) =>
        await context.BumpSessionVersionAsync(cancellationToken);

    /// <inheritdoc />
    protected override async Task AfterDeleteAsync(AdminRole entity, CrudDeleteDecision decision, CancellationToken cancellationToken) =>
        await context.BumpSessionVersionAsync(cancellationToken);

    /// <inheritdoc />
    protected override HarborErrorDefinition NotFoundError => AdminRoleErrorCodes.NotFound;

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
    private async Task<HarborResult> ValidateMenusAsync(IReadOnlyList<long> menuIds, CancellationToken cancellationToken)
    {
        if (menuIds.Count == 0)
        {
            return HarborResult.Success();
        }

        var menus = await menuRepository.GetMenusByIdsAsync(menuIds, cancellationToken);
        var existingIds = menus.Select(menu => menu.Id).ToHashSet();
        var missingIds = menuIds.Where(menuId => !existingIds.Contains(menuId)).ToArray();
        if (missingIds.Length > 0)
        {
            return HarborResult.Failure(AdminRoleErrorCodes.MenuNotFound.Create(
                new Dictionary<string, object?> { ["ids"] = string.Join(", ", missingIds) }));
        }

        return HarborResult.Success();
    }
}
