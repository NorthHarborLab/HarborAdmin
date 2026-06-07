using HarborAdmin.Modules.Admin.Contracts.Access.Dto;

namespace HarborAdmin.Modules.Admin.Application.Services.Access;

/// <summary>
/// 用户字段权限策略服务。
/// </summary>
public sealed class FieldPolicyService(AccessCacheService accessCache)
{
    /// <summary>
    /// 获取指定用户的字段策略，供动态页面字段权限使用。
    /// </summary>
    public async Task<IReadOnlyList<FieldPolicyDto>> GetFieldPoliciesForUserAsync(long userId, CancellationToken cancellationToken)
    {
        var snapshot = await accessCache.GetUserSnapshotAsync(userId, cancellationToken);
        return snapshot.IsSuperAdmin ? [] : snapshot.FieldPolicies;
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
    public Task<IReadOnlyList<FieldPolicyDto>> GetFieldPoliciesAsync(long[] roleIds, CancellationToken cancellationToken) =>
        accessCache.MergeRoleFieldPoliciesAsync(roleIds, cancellationToken);
}
