using HarborAdmin.Modules.Admin.Application.Abstractions;
using HarborAdmin.Modules.Admin.Domain.Entities;

namespace HarborAdmin.Modules.Admin.Application.Services.Shared;

/// <summary>
/// Admin 系统管理服务上下文与共享能力。
/// </summary>
public sealed class SystemServiceContext(IAdminUserRepository userRepository, IAdminMenuRepository menuRepository, AdminServiceContext adminContext)
{
    /// <summary>
    /// Admin 共享服务上下文。
    /// </summary>
    public AdminServiceContext AdminContext { get; } = adminContext;

    /// <summary>
    /// 加载用户聚合。
    /// </summary>
    public Task<AdminUser?> LoadUserAggregateAsync(long userId, CancellationToken cancellationToken) =>
        userRepository.GetUserAggregateAsync(userId, cancellationToken);

    /// <summary>
    /// 保存用户。
    /// </summary>
    public Task SaveUserAsync(AdminUser user, bool isUpdate, CancellationToken cancellationToken) =>
        userRepository.SaveUserAsync(user, isUpdate, cancellationToken);

    /// <summary>
    /// 保存菜单。
    /// </summary>
    public Task SaveMenuAsync(AdminMenu menu, bool isUpdate, CancellationToken cancellationToken) =>
        menuRepository.SaveMenuAsync(menu, isUpdate, cancellationToken);

    /// <summary>
    /// 保存用户子集合。
    /// </summary>
    public void SaveUserChildren(AdminUser user, string propertyName) =>
        userRepository.SaveUserChildren(user, propertyName);

    /// <summary>
    /// 级联删除用户。
    /// </summary>
    public Task DeleteUserCascadeAsync(long userId, CancellationToken cancellationToken) =>
        userRepository.DeleteUserCascadeAsync(userId, cancellationToken);

    /// <summary>
    /// 级联删除菜单。
    /// </summary>
    public Task DeleteMenuCascadeAsync(long menuId, CancellationToken cancellationToken) =>
        menuRepository.DeleteMenuCascadeAsync(menuId, cancellationToken);

    /// <summary>
    /// 按功能编码解析功能。
    /// </summary>
    public async Task<AdminFeature?> ResolveFeatureByCodeAsync(string? featureCode, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(featureCode))
        {
            return null;
        }

        return await menuRepository.ResolveFeatureByCodeAsync(featureCode, cancellationToken);
    }
}
