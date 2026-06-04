using HarborAdmin.Modules.International.Domain.Entities;

namespace HarborAdmin.Modules.International.Application.Abstractions;

/// <summary>
/// 国际化页面仓储接口。
/// </summary>
public partial interface IInternationalRepository
{
    /// <summary>
    /// 列出所有国际化页面。
    /// </summary>
    Task<IReadOnlyList<InternationalPage>> ListPagesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 列出页面并加载条目与翻译。
    /// </summary>
    Task<IReadOnlyList<InternationalPage>> ListPagesWithEntriesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 按主键获取页面。
    /// </summary>
    Task<InternationalPage?> GetPageAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 按页面 Key 获取页面。
    /// </summary>
    Task<InternationalPage?> GetPageByKeyAsync(string pageKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// 按页面 Key 获取页面并加载条目与翻译。
    /// </summary>
    Task<InternationalPage?> GetPageWithEntriesByKeyAsync(string pageKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// 新增页面。
    /// </summary>
    Task<InternationalPage> InsertPageAsync(InternationalPage page, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新页面。
    /// </summary>
    Task UpdatePageAsync(InternationalPage page, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除页面及其全部条目和翻译。
    /// </summary>
    Task DeletePageAsync(long id, CancellationToken cancellationToken = default);
}
