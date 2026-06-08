using HarborAdmin.BuildingBlocks.Abstractions.Exception;
using HarborAdmin.BuildingBlocks.Mapping;
using HarborAdmin.Modules.Admin.Application.Abstractions;
using HarborAdmin.Modules.Admin.Contracts.Access.Dto;
using HarborAdmin.Modules.Admin.Contracts.System.Dto;
using HarborAdmin.Modules.Admin.Contracts.System.Request;
using HarborAdmin.Modules.Admin.Domain.Entities;
using HarborAdmin.Modules.Admin.Application.Services.Access;
using HarborAdmin.Modules.Admin.Application.Services.Shared;
using Microsoft.AspNetCore.Identity;

namespace HarborAdmin.Modules.Admin.Application.Services.User;

/// <summary>
/// 用户管理服务。
/// </summary>
public sealed class UserService(
    SystemServiceContext systemContext,
    AdminServiceContext context,
    IAdminRepository repository,
    AccessQueryService accessQuery,
    FieldPolicyService fieldPolicyService,
    IHarborMapper mapper)
{
    private readonly PasswordHasher<AdminUser> _passwordHasher = new();

    /// <summary>
    /// 根据用户 ID 获取用户实体。
    /// </summary>
    public async Task<AdminUser?> GetUserAsync(long userId, CancellationToken cancellationToken) =>
        await repository.GetUserAggregateAsync(userId, cancellationToken);

    /// <summary>
    /// 按数据范围获取用户列表，并应用字段脱敏策略。
    /// </summary>
    public async Task<IReadOnlyList<SystemUserDto>> ListUsersAsync(long currentUserId, long? deptId, CancellationToken cancellationToken)
    {
        var users = await repository.ListUsersWithRolesAsync(cancellationToken);
        var allowedDeptIds = await accessQuery.GetAllowedDepartmentIdsAsync(currentUserId, cancellationToken);
        if (allowedDeptIds is not null)
        {
            users = users.Where(user => user.Id == currentUserId || (user.DeptId.HasValue && allowedDeptIds.Contains(user.DeptId.Value))).ToList();
        }

        if (deptId.HasValue)
        {
            users = users.Where(user => user.DeptId == deptId.Value).ToList();
        }

        var policies = await fieldPolicyService.GetPoliciesForFeatureAsync(currentUserId, "system.user", cancellationToken);
        return users.Select(user => ApplyUserFieldPolicies(mapper.Map<SystemUserDto>(user), policies)).ToArray();
    }

    /// <summary>
    /// 新增或更新用户，并同步角色关联。
    /// </summary>
    public async Task<SystemUserDto> SaveUserAsync(long currentUserId, long? id, SaveSystemUserRequest request, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        AdminUser user;
        if (id.HasValue)
        {
            user = await systemContext.LoadUserAggregateAsync(id.Value, cancellationToken)
                   ?? throw new NotFoundDomainException("用户不存在。");
        }
        else
        {
            user = new AdminUser { CreatedAt = now };
        }

        user.UserName = string.IsNullOrWhiteSpace(request.UserName) ? AdminIdHelper.BuildCode(request.Name) : request.UserName;
        user.DisplayName = request.Name;
        user.DeptId = AdminIdHelper.ParseNullableId(request.DeptId);
        user.Remark = request.Remark;
        user.Enabled = request.Status == 1;
        user.HomePath ??= "/dashboard";
        user.UpdatedAt = now;
        await ApplySuperAdminFlagAsync(currentUserId, user, request.IsSuperAdmin, cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.Password) || !id.HasValue)
        {
            user.PasswordHash = _passwordHasher.HashPassword(user, string.IsNullOrWhiteSpace(request.Password) ? "HarborAdmin@123456" : request.Password);
        }

        var userRepository = systemContext.GetUserRepository();
        if (id.HasValue)
        {
            await userRepository.UpdateAsync(user, cancellationToken);
        }
        else
        {
            await userRepository.InsertAsync(user, cancellationToken);
        }

        var roleIds = (request.RoleIds ?? request.Permissions ?? [])
            .Select(AdminIdHelper.ParseId)
            .Distinct()
            .ToArray();
        user.UserRoles = roleIds
            .Select(roleId => new AdminUserRole { UserId = user.Id, RoleId = roleId })
            .ToList();
        systemContext.SaveUserChildren(user, nameof(AdminUser.UserRoles));

        await context.BumpSessionVersionAsync(cancellationToken);
        user = await systemContext.LoadUserAggregateAsync(user.Id, cancellationToken)
               ?? throw new NotFoundDomainException("用户不存在。");
        return mapper.Map<SystemUserDto>(user);
    }

    /// <summary>
    /// 删除用户及其角色、刷新令牌关联。
    /// </summary>
    public async Task DeleteUserAsync(long id, CancellationToken cancellationToken)
    {
        _ = await systemContext.LoadUserAggregateAsync(id, cancellationToken)
            ?? throw new NotFoundDomainException("用户不存在。");
        await systemContext.GetUserRepository().DeleteCascadeByDatabaseAsync(user => user.Id == id, cancellationToken);
        await context.BumpSessionVersionAsync(cancellationToken);
    }

    private async Task ApplySuperAdminFlagAsync(long currentUserId, AdminUser user, bool requested, CancellationToken cancellationToken)
    {
        if (requested && !await accessQuery.IsSuperAdminAsync(currentUserId, cancellationToken))
        {
            throw new ValidationDomainException("无权设置超级管理员。");
        }

        if (await accessQuery.IsSuperAdminAsync(currentUserId, cancellationToken))
        {
            user.IsSuperAdmin = requested;
        }
    }

    private static SystemUserDto ApplyUserFieldPolicies(SystemUserDto user, IReadOnlyList<FieldPolicyDto> policies)
    {
        var hidden = policies.Where(policy => !policy.Visible).Select(policy => policy.FieldName).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return user with
        {
            Remark = hidden.Contains("remark") ? null : user.Remark,
        };
    }
}