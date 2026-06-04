using HarborAdmin.Modules.International.Domain.Entities;

namespace HarborAdmin.Modules.International.Application.Abstractions;

/// <summary>
/// 国际化版本仓储接口。
/// </summary>
public partial interface IInternationalRepository
{
    /// <summary>
    /// 列出页面 Key 与页面版本。
    /// </summary>
    Task<IReadOnlyList<InternationalPage>> ListPageVersionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 计算当前国际化资源总版本。
    /// </summary>
    Task<int> GetVersionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 增加指定页面版本。
    /// </summary>
    Task IncreasePageVersionAsync(long pageId, CancellationToken cancellationToken = default);
}
