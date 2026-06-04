using HarborAdmin.Modules.AI.Domain.Entities;

namespace HarborAdmin.Modules.AI.Application.Abstractions;

public partial interface IAiRepository
{
    /// <summary>
    /// 列出供应商。
    /// </summary>
    Task<IReadOnlyList<AiProvider>> ListProvidersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 按 Key 获取供应商。
    /// </summary>
    Task<AiProvider?> GetProviderByKeyAsync(string providerKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// 按主键获取供应商。
    /// </summary>
    Task<AiProvider?> GetProviderAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 保存供应商。
    /// </summary>
    Task<AiProvider> SaveProviderAsync(AiProvider provider, IReadOnlyList<AiProviderModel> models, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除供应商。
    /// </summary>
    Task DeleteProviderAsync(long id, CancellationToken cancellationToken = default);
}
