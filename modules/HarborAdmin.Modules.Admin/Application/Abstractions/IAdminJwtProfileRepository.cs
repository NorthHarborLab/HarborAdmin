using HarborAdmin.Modules.Admin.Domain.Entities;

namespace HarborAdmin.Modules.Admin.Application.Abstractions;

/// <summary>
/// JWT Profile 仓储。
/// </summary>
public interface IAdminJwtProfileRepository
{
    /// <summary>
    /// 列出 JWT Profile。
    /// </summary>
    Task<IReadOnlyList<AdminJwtProfile>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 按 Profile Key 获取 JWT Profile。
    /// </summary>
    Task<AdminJwtProfile?> GetByProfileKeyAsync(string profileKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// 保存 JWT Profile。
    /// </summary>
    Task SaveAsync(AdminJwtProfile profile, bool isUpdate, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新 JWT Profile。
    /// </summary>
    Task UpdateAsync(AdminJwtProfile profile, CancellationToken cancellationToken = default);
}
