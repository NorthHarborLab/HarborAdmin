using HarborAdmin.BuildingBlocks.Data;
using HarborAdmin.Modules.Admin.Application.Abstractions;
using HarborAdmin.Modules.Admin.Domain.Entities;
using HarborAdmin.Modules.Admin.Infrastructure.Contexts;

namespace HarborAdmin.Modules.Admin.Infrastructure.Repositories;

/// <summary>
/// Admin 字典 FreeSql 仓储。
/// </summary>
public sealed class AdminDictionaryRepository(IAdminDbContext db) : FreeSqlModuleRepository<IAdminDbContext>(db), IAdminDictionaryRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<AdminDictionary>> ListDictionariesAsync(string? keyword, CancellationToken cancellationToken = default)
    {
        var query = FreeSql.Select<AdminDictionary>();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(item => item.DictCode.Contains(keyword) || item.Name.Contains(keyword));
        }

        return await query
            .OrderBy(item => item.SortOrder)
            .OrderBy(item => item.DictCode)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> DictionaryExistsAsync(string dictCode, CancellationToken cancellationToken = default) =>
        FreeSql.Select<AdminDictionary>()
            .Where(item => item.DictCode == dictCode)
            .AnyAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<AdminDictionary?> GetDictionaryByCodeAsync(string dictCode, CancellationToken cancellationToken = default) =>
        await FreeSql.Select<AdminDictionary>()
            .Where(item => item.DictCode == dictCode)
            .ToOneAsync(cancellationToken);

    /// <inheritdoc />
    public Task InsertDictionaryAsync(AdminDictionary dictionary, CancellationToken cancellationToken = default) =>
        FreeSql.Insert(dictionary).ExecuteAffrowsAsync(cancellationToken);

    /// <inheritdoc />
    public Task UpdateDictionaryAsync(AdminDictionary dictionary, CancellationToken cancellationToken = default) =>
        FreeSql.Update<AdminDictionary>().SetSource(dictionary).ExecuteAffrowsAsync(cancellationToken);

    /// <inheritdoc />
    public async Task DeleteDictionaryWithItemsAsync(AdminDictionary dictionary, CancellationToken cancellationToken = default)
    {
        await FreeSql.Delete<AdminDictionaryItem>()
            .Where(item => item.DictCode == dictionary.DictCode)
            .ExecuteAffrowsAsync(cancellationToken);
        await FreeSql.Delete<AdminDictionary>()
            .Where(item => item.Id == dictionary.Id)
            .ExecuteAffrowsAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AdminDictionaryItem>> ListItemsAsync(string dictCode, CancellationToken cancellationToken = default) =>
        await FreeSql.Select<AdminDictionaryItem>()
            .Where(item => item.DictCode == dictCode)
            .OrderBy(item => item.SortOrder)
            .OrderBy(item => item.ItemValue)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<AdminDictionaryItem>> ListEnabledItemsAsync(string dictCode, CancellationToken cancellationToken = default) =>
        await FreeSql.Select<AdminDictionaryItem>()
            .Where(item => item.DictCode == dictCode && item.Enabled)
            .OrderBy(item => item.SortOrder)
            .OrderBy(item => item.ItemValue)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public Task<bool> DictionaryItemExistsAsync(string dictCode, string itemValue, CancellationToken cancellationToken = default) =>
        FreeSql.Select<AdminDictionaryItem>()
            .Where(item => item.DictCode == dictCode && item.ItemValue == itemValue)
            .AnyAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<AdminDictionaryItem?> GetItemAsync(string dictCode, long itemId, CancellationToken cancellationToken = default) =>
        await FreeSql.Select<AdminDictionaryItem>()
            .Where(item => item.Id == itemId && item.DictCode == dictCode)
            .ToOneAsync(cancellationToken);

    /// <inheritdoc />
    public Task InsertItemAsync(AdminDictionaryItem item, CancellationToken cancellationToken = default) =>
        FreeSql.Insert(item).ExecuteAffrowsAsync(cancellationToken);

    /// <inheritdoc />
    public Task UpdateItemAsync(AdminDictionaryItem item, CancellationToken cancellationToken = default) =>
        FreeSql.Update<AdminDictionaryItem>().SetSource(item).ExecuteAffrowsAsync(cancellationToken);

    /// <inheritdoc />
    public Task DeleteItemAsync(string dictCode, long itemId, CancellationToken cancellationToken = default) =>
        FreeSql.Delete<AdminDictionaryItem>()
            .Where(item => item.Id == itemId && item.DictCode == dictCode)
            .ExecuteAffrowsAsync(cancellationToken);
}
