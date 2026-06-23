using HarborAdmin.Modules.Admin.Domain.Entities;

namespace HarborAdmin.Modules.Admin.Application.Abstractions;

/// <summary>
/// JWT Profile 刷新令牌仓储。
/// </summary>
public interface IAdminJwtRefreshTokenRepository
{
    /// <summary>
    /// 按刷新令牌哈希获取记录。
    /// </summary>
    /// <param name="tokenHash">刷新令牌哈希。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>刷新令牌记录。</returns>
    Task<AdminJwtRefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// 新增刷新令牌。
    /// </summary>
    /// <param name="token">刷新令牌记录。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task InsertAsync(AdminJwtRefreshToken token, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新刷新令牌。
    /// </summary>
    /// <param name="token">刷新令牌记录。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task UpdateAsync(AdminJwtRefreshToken token, CancellationToken cancellationToken = default);
}
