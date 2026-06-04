using HarborAdmin.Modules.AI.Domain.Entities;

namespace HarborAdmin.Modules.AI.Application.Abstractions;

public partial interface IAiRepository
{
    /// <summary>
    /// 列出业务。
    /// </summary>
    Task<IReadOnlyList<AiBusiness>> ListBusinessesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 按 Key 获取业务。
    /// </summary>
    Task<AiBusiness?> GetBusinessByKeyAsync(string businessKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// 按主键获取业务。
    /// </summary>
    Task<AiBusiness?> GetBusinessAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 保存业务。
    /// </summary>
    Task<AiBusiness> SaveBusinessAsync(AiBusiness business, IReadOnlyList<AiBusinessProviderRoute> routes, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除业务。
    /// </summary>
    Task DeleteBusinessAsync(long id, CancellationToken cancellationToken = default);
}
