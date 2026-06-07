using HarborAdmin.Modules.Admin.Contracts.Access.Dto;
using HarborAdmin.Modules.Admin.Application.Services.Shared;

namespace HarborAdmin.Modules.Admin.Application.Services.Access;

/// <summary>
/// 用户字段权限策略服务。
/// </summary>
public sealed class FieldPolicyService(AdminServiceContext context, AccessQueryService accessQuery)
{
    private IFreeSql Orm => context.Orm;

    /// <summary>
    /// 获取指定用户的字段策略，供动态页面字段权限使用。
    /// </summary>
    public async Task<IReadOnlyList<FieldPolicyDto>> GetFieldPoliciesForUserAsync(long userId, CancellationToken cancellationToken)
    {
        var roles = await accessQuery.GetEnabledUserRolesAsync(userId, cancellationToken);
        var roleIds = roles.Select(role => role.Id).ToArray();
        return await GetFieldPoliciesAsync(roleIds, cancellationToken);
    }

    /// <summary>
    /// 获取用户在指定功能下的字段策略子集。
    /// </summary>
    public async Task<IReadOnlyList<FieldPolicyDto>> GetPoliciesForFeatureAsync(long userId, string featureCode, CancellationToken cancellationToken)
    {
        var policies = await GetFieldPoliciesForUserAsync(userId, cancellationToken);
        return policies.Where(policy => policy.FeatureCode == featureCode).ToArray();
    }

    /// <summary>
    /// 按角色合并字段策略，多角色取并集。
    /// </summary>
    public async Task<IReadOnlyList<FieldPolicyDto>> GetFieldPoliciesAsync(long[] roleIds, CancellationToken cancellationToken)
    {
        if (roleIds.Length == 0)
        {
            return [];
        }

        var policies = await Orm.Select<Domain.Entities.AdminRoleFieldPermission>()
            .Where(policy => roleIds.Contains(policy.RoleId))
            .ToListAsync(cancellationToken);
        return policies
            .GroupBy(policy => (policy.FeatureCode, policy.FieldName))
            .Select(group =>
            {
                var items = group.ToArray();
                return new FieldPolicyDto(
                    group.Key.FeatureCode,
                    group.Key.FieldName,
                    items.Any(item => item.Visible),
                    items.Any(item => item.Editable),
                    items.Any(item => item.Exportable),
                    items.Any(item => item.Masked));
            })
            .ToArray();
    }
}
