using HarborAdmin.Modules.International.Domain;

namespace HarborAdmin.Modules.International.Contracts;

/// <summary>
/// 国际化持久化仓储接口
/// </summary>
public interface IInternationalRepository
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
    /// 列出页面 Key 与页面版本。
    /// </summary>
    Task<IReadOnlyList<InternationalPage>> ListPageVersionsAsync(CancellationToken cancellationToken = default);

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

    /// <summary>
    /// 列出指定页面的条目与翻译。
    /// </summary>
    Task<IReadOnlyList<InternationalEntry>> ListEntriesAsync(long pageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 按主键获取条目与翻译。
    /// </summary>
    Task<InternationalEntry?> GetEntryAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 新增条目及其翻译。
    /// </summary>
    Task<InternationalEntry> InsertEntryAsync(InternationalEntry entry, IReadOnlyList<InternationalEntryTranslation> translations, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新条目并替换其全部翻译。
    /// </summary>
    Task UpdateEntryAsync(InternationalEntry entry, IReadOnlyList<InternationalEntryTranslation> translations, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除条目及其子树。
    /// </summary>
    Task DeleteEntryAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 计算当前国际化资源总版本。
    /// </summary>
    Task<int> GetVersionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 增加指定页面版本。
    /// </summary>
    Task IncreasePageVersionAsync(long pageId, CancellationToken cancellationToken = default);
}
