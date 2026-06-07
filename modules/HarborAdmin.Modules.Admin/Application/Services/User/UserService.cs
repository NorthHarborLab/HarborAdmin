using HarborAdmin.BuildingBlocks.Abstractions.Exception;
using HarborAdmin.Modules.Admin.Contracts.Auth.Dto;
using HarborAdmin.Modules.Admin.Contracts.System;
using HarborAdmin.Modules.Admin.Domain.Entities;
using HarborAdmin.Modules.Admin.Application.Services.FieldPolicy;
using HarborAdmin.Modules.Admin.Application.Services.Shared;
using Microsoft.AspNetCore.Identity;

namespace HarborAdmin.Modules.Admin.Application.Services.User;

/// <summary>
/// 用户管理服务。
/// </summary>
public sealed class UserService(AdminServiceContext context, AccessQueryService accessQuery, FieldPolicyService fieldPolicyService)
{
    private readonly PasswordHasher<AdminUser> _passwordHasher = new();
    private IFreeSql Orm => context.Orm;

    /// <summary>
    /// 根据用户 ID 获取用户实体。
    /// </summary>
    public async Task<AdminUser?> GetUserAsync(long userId, CancellationToken cancellationToken) =>
        await Orm.Select<AdminUser>().Where(user => user.Id == userId).ToOneAsync(cancellationToken);

    /// <summary>
    /// 按数据范围获取用户列表，并应用字段脱敏策略。
    /// </summary>
    public async Task<IReadOnlyList<SystemUserDto>> ListUsersAsync(long currentUserId, long? deptId, CancellationToken cancellationToken)
    {
        var users = await Orm.Select<AdminUser>().OrderBy(user => user.Id).ToListAsync(cancellationToken);
        var allowedDeptIds = await accessQuery.GetAllowedDepartmentIdsAsync(currentUserId, cancellationToken);
        if (allowedDeptIds is not null)
        {
            users = users.Where(user => user.Id == currentUserId || (user.DeptId.HasValue && allowedDeptIds.Contains(user.DeptId.Value))).ToList();
        }

        if (deptId.HasValue)
        {
            users = users.Where(user => user.DeptId == deptId.Value).ToList();
        }

        var userRoles = await Orm.Select<AdminUserRole>().ToListAsync(cancellationToken);
        var policies = await fieldPolicyService.GetPoliciesForFeatureAsync(currentUserId, "system.user", cancellationToken);
        return users.Select(user => ApplyUserFieldPolicies(ToUserDto(user, userRoles), policies)).ToArray();
    }

    /// <summary>
    /// 新增或更新用户，并同步角色关联。
    /// </summary>
    public async Task<SystemUserDto> SaveUserAsync(long? id, SaveSystemUserRequest request, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var user = id.HasValue
            ? await Orm.Select<AdminUser>().Where(item => item.Id == id).ToOneAsync(cancellationToken)
              ?? throw new NotFoundDomainException("用户不存在。")
            : new AdminUser { CreatedAt = now };
        user.UserName = string.IsNullOrWhiteSpace(request.UserName) ? AdminIdHelper.BuildCode(request.Name) : request.UserName;
        user.DisplayName = request.Name;
        user.DeptId = AdminIdHelper.ParseNullableId(request.DeptId);
        user.Remark = request.Remark;
        user.Enabled = request.Status == 1;
        user.HomePath ??= "/dashboard";
        user.UpdatedAt = now;

        if (!string.IsNullOrWhiteSpace(request.Password) || !id.HasValue)
        {
            user.PasswordHash = _passwordHasher.HashPassword(user, string.IsNullOrWhiteSpace(request.Password) ? "HarborAdmin@123456" : request.Password);
        }

        if (id.HasValue)
        {
            await Orm.Update<AdminUser>().SetSource(user).ExecuteAffrowsAsync(cancellationToken);
        }
        else
        {
            await Orm.Insert(user).ExecuteAffrowsAsync(cancellationToken);
        }

        await ReplaceUserRolesAsync(user.Id, request.RoleIds ?? request.Permissions ?? [], cancellationToken);
        await context.BumpSessionVersionAsync(cancellationToken);
        var userRoles = await Orm.Select<AdminUserRole>().ToListAsync(cancellationToken);
        return ToUserDto(user, userRoles);
    }

    /// <summary>
    /// 删除用户及其角色、刷新令牌关联。
    /// </summary>
    public async Task DeleteUserAsync(long id, CancellationToken cancellationToken)
    {
        await Orm.Delete<AdminUserRole>().Where(link => link.UserId == id).ExecuteAffrowsAsync(cancellationToken);
        await Orm.Delete<AdminRefreshToken>().Where(token => token.UserId == id).ExecuteAffrowsAsync(cancellationToken);
        await Orm.Delete<AdminUser>().Where(user => user.Id == id).ExecuteAffrowsAsync(cancellationToken);
        await context.BumpSessionVersionAsync(cancellationToken);
    }

    private async Task ReplaceUserRolesAsync(long userId, IReadOnlyList<string> selectedValues, CancellationToken cancellationToken)
    {
        await Orm.Delete<AdminUserRole>().Where(link => link.UserId == userId).ExecuteAffrowsAsync(cancellationToken);
        var roleIds = selectedValues.Select(AdminIdHelper.ParseId).Distinct().ToArray();
        if (roleIds.Length > 0)
        {
            await Orm.Insert(roleIds.Select(roleId => new AdminUserRole { UserId = userId, RoleId = roleId })).ExecuteAffrowsAsync(cancellationToken);
        }
    }

    private static SystemUserDto ToUserDto(AdminUser user, IReadOnlyList<AdminUserRole> userRoles)
    {
        var roleIds = userRoles.Where(link => link.UserId == user.Id).Select(link => link.RoleId.ToString()).ToArray();
        return new SystemUserDto(
            user.Id.ToString(),
            user.DisplayName,
            user.UserName,
            user.DeptId?.ToString(),
            roleIds,
            roleIds,
            user.Remark,
            user.Enabled ? 1 : 0,
            user.CreatedAt.ToString("O"));
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
