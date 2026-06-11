using HarborAdmin.Modules.Admin.Domain.Entities;

namespace HarborAdmin.Modules.Admin.Application.Abstractions;

/// <summary>
/// Admin 用户聚合仓储。
/// </summary>
public interface IAdminUserRepository
{
    /// <summary>
    /// 加载用户聚合（部门 + 角色）。
    /// </summary>
    Task<AdminUser?> GetUserAggregateAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 加载用户列表（含角色关系）。
    /// </summary>
    Task<IReadOnlyList<AdminUser>> ListUsersWithRolesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 保存用户。
    /// </summary>
    Task SaveUserAsync(AdminUser user, bool isUpdate, CancellationToken cancellationToken = default);

    /// <summary>
    /// 保存用户子集合。
    /// </summary>
    void SaveUserChildren(AdminUser user, string propertyName);

    /// <summary>
    /// 级联删除用户。
    /// </summary>
    Task DeleteUserCascadeAsync(long userId, CancellationToken cancellationToken = default);
}
