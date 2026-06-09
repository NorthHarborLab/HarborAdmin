using HarborAdmin.Modules.International.Domain.Entities;

namespace HarborAdmin.Modules.International.Infrastructure.Repositories;

/// <summary>
/// 基于 FreeSql 的国际化资源分组仓储实现。
/// </summary>
public sealed partial class FreeSqlInternationalRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<InternationalGroup>> ListGroupsAsync(CancellationToken cancellationToken = default) =>
        await FreeSql.Select<InternationalGroup>()
            .OrderBy(group => group.Path)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<InternationalGroup?> GetGroupAsync(long id, CancellationToken cancellationToken = default) =>
        await FreeSql.Select<InternationalGroup>()
            .Where(group => group.Id == id)
            .FirstAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<InternationalGroup?> GetGroupByPathAsync(string path, CancellationToken cancellationToken = default) =>
        await FreeSql.Select<InternationalGroup>()
            .Where(group => group.Path == path)
            .FirstAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<InternationalGroup> InsertGroupAsync(InternationalGroup group, CancellationToken cancellationToken = default)
    {
        var inserted = await FreeSql.Insert(group).ExecuteInsertedAsync(cancellationToken);
        group.Id = inserted.First().Id;
        return group;
    }

    /// <inheritdoc />
    public Task UpdateGroupsAsync(IReadOnlyList<InternationalGroup> groups, CancellationToken cancellationToken = default) =>
        groups.Count == 0
            ? Task.CompletedTask
            : FreeSql.Update<InternationalGroup>().SetSource(groups).ExecuteAffrowsAsync(cancellationToken);
}
