using HarborAdmin.Modules.Admin.Contracts.System.Dto;
using HarborAdmin.Modules.Admin.Domain.Entities;
using Mapster;

namespace HarborAdmin.Modules.Admin.Application.Mappings;

/// <summary>
/// 系统管理 Mapster 映射配置。
/// </summary>
public sealed class AdminSystemMappingRegister : IRegister
{
    /// <inheritdoc />
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<AdminUser, SystemUserDto>()
            .Map(destination => destination.Id, source => source.Id.ToString())
            .Map(destination => destination.Name, source => source.DisplayName)
            .Map(destination => destination.DeptId, source => source.DeptId.HasValue ? source.DeptId.Value.ToString() : null)
            .Map(destination => destination.RoleIds, source => source.UserRoles.Select(link => link.RoleId.ToString()).ToArray())
            .Map(destination => destination.Permissions, source => source.UserRoles.Select(link => link.RoleId.ToString()).ToArray())
            .Map(destination => destination.Status, source => source.Enabled ? 1 : 0)
            .Map(destination => destination.CreateTime, source => source.CreatedAt.ToString("O"));

        config.NewConfig<AdminRole, SystemRoleDto>()
            .Map(destination => destination.Id, source => source.Id.ToString())
            .Map(destination => destination.MenuIds, source => source.RoleMenus.Select(link => link.MenuId.ToString()).ToArray())
            .Map(destination => destination.PermissionCodes, source => source.RolePermissions.Select(link => link.PermissionCode).ToArray())
            .Map(destination => destination.FieldPolicies, source => source.FieldPermissions)
            .Map(destination => destination.Permissions, source =>
                source.RoleMenus.Select(link => link.MenuId.ToString())
                    .Concat(source.RolePermissions.Select(link => $"perm:{link.PermissionCode}"))
                    .ToArray())
            .Map(destination => destination.Status, source => source.Enabled ? 1 : 0)
            .Map(destination => destination.CreateTime, source => source.CreatedAt.ToString("O"));

        config.NewConfig<AdminRoleFieldPermission, SystemRoleFieldPolicyDto>();

        config.NewConfig<AdminDepartment, SystemDeptDto>()
            .Map(destination => destination.Id, source => source.Id.ToString())
            .Map(destination => destination.Pid, source => source.ParentId.HasValue ? source.ParentId.Value.ToString() : "0")
            .Map(destination => destination.Status, source => source.Enabled ? 1 : 0)
            .Ignore(destination => destination.Children);
    }
}
