using HarborAdmin.Modules.Admin.Domain.Entities;

namespace HarborAdmin.Modules.Admin.Application.Abstractions;

/// <summary>
/// Admin 匿名认证仓储。
/// </summary>
public interface IAdminAuthRepository
{
    /// <summary>
    /// 按用户名获取用户。
    /// </summary>
    Task<AdminUser?> GetUserByUserNameAsync(string userName, CancellationToken cancellationToken = default);

    /// <summary>
    /// 按用户 ID 获取用户。
    /// </summary>
    Task<AdminUser?> GetUserByIdAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新用户密码哈希。
    /// </summary>
    Task UpdateUserPasswordHashAsync(AdminUser user, CancellationToken cancellationToken = default);

    /// <summary>
    /// 按令牌哈希获取刷新令牌。
    /// </summary>
    Task<AdminRefreshToken?> GetRefreshTokenByHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// 新增刷新令牌。
    /// </summary>
    Task InsertRefreshTokenAsync(AdminRefreshToken token, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新刷新令牌。
    /// </summary>
    Task UpdateRefreshTokenAsync(AdminRefreshToken token, CancellationToken cancellationToken = default);
}
