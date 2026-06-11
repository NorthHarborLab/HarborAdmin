using HarborAdmin.Modules.International.Domain.Entities;

namespace HarborAdmin.Modules.International.Application.Abstractions;

/// <summary>
/// 国际化资源分组仓储接口。
/// </summary>
public interface IInternationalGroupRepository
{
    /// <summary>
    /// 列出所有国际化资源分组。
    /// </summary>
    Task<IReadOnlyList<InternationalGroup>> ListGroupsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 按主键获取资源分组。
    /// </summary>
    Task<InternationalGroup?> GetGroupAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 按路径获取资源分组。
    /// </summary>
    Task<InternationalGroup?> GetGroupByPathAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// 新增资源分组。
    /// </summary>
    Task<InternationalGroup> InsertGroupAsync(InternationalGroup group, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量更新资源分组。
    /// </summary>
    Task UpdateGroupsAsync(IReadOnlyList<InternationalGroup> groups, CancellationToken cancellationToken = default);
}
