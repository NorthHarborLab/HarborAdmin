using HarborAdmin.Modules.Admin.Domain.Entities;

namespace HarborAdmin.Modules.Admin.Application.Abstractions;

/// <summary>
/// Admin 字典仓储。
/// </summary>
public interface IAdminDictionaryRepository
{
    /// <summary>
    /// 查询字典类型。
    /// </summary>
    Task<IReadOnlyList<AdminDictionary>> ListDictionariesAsync(string? keyword, CancellationToken cancellationToken = default);

    /// <summary>
    /// 判断字典编码是否存在。
    /// </summary>
    Task<bool> DictionaryExistsAsync(string dictCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// 按编码获取字典。
    /// </summary>
    Task<AdminDictionary?> GetDictionaryByCodeAsync(string dictCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// 新增字典。
    /// </summary>
    Task InsertDictionaryAsync(AdminDictionary dictionary, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新字典。
    /// </summary>
    Task UpdateDictionaryAsync(AdminDictionary dictionary, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除字典及其字典项。
    /// </summary>
    Task DeleteDictionaryWithItemsAsync(AdminDictionary dictionary, CancellationToken cancellationToken = default);

    /// <summary>
    /// 查询字典项。
    /// </summary>
    Task<IReadOnlyList<AdminDictionaryItem>> ListItemsAsync(string dictCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// 查询已启用字典项。
    /// </summary>
    Task<IReadOnlyList<AdminDictionaryItem>> ListEnabledItemsAsync(string dictCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// 判断字典项值是否存在。
    /// </summary>
    Task<bool> DictionaryItemExistsAsync(string dictCode, string itemValue, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取字典项。
    /// </summary>
    Task<AdminDictionaryItem?> GetItemAsync(string dictCode, long itemId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 新增字典项。
    /// </summary>
    Task InsertItemAsync(AdminDictionaryItem item, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新字典项。
    /// </summary>
    Task UpdateItemAsync(AdminDictionaryItem item, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除字典项。
    /// </summary>
    Task DeleteItemAsync(string dictCode, long itemId, CancellationToken cancellationToken = default);
}
