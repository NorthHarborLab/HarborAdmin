using HarborAdmin.Modules.International.Domain.Entities;

namespace HarborAdmin.Modules.International.Application.Abstractions;

/// <summary>
/// 国际化条目仓储接口。
/// </summary>
public partial interface IInternationalRepository
{
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
    /// 更新条目的部分翻译。
    /// </summary>
    Task UpsertEntryTranslationsAsync(long entryId, IReadOnlyList<InternationalEntryTranslation> translations, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除条目及其子树。
    /// </summary>
    Task DeleteEntryAsync(long id, CancellationToken cancellationToken = default);
}
