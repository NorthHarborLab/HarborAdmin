using HarborAdmin.Modules.Admin.Contracts.DynamicCurd.Dtos;
using HarborAdmin.Modules.Admin.Contracts.DynamicCurd.Requests;

namespace HarborAdmin.Modules.Admin.Application.Abstractions;

/// <summary>
/// Admin 动态资源 CRUD 处理器。
/// </summary>
public interface IAdminDynamicResourceHandler
{
    /// <summary>
    /// 处理器标识。
    /// </summary>
    string HandlerKey { get; }

    /// <summary>
    /// 分页查询动态资源记录。
    /// </summary>
    Task<DynamicQueryResultDto> QueryAsync(DynamicQueryRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// 获取动态资源记录详情。
    /// </summary>
    Task<IReadOnlyDictionary<string, object?>?> GetAsync(string id, CancellationToken cancellationToken);

    /// <summary>
    /// 新增动态资源记录。
    /// </summary>
    Task<IReadOnlyDictionary<string, object?>> CreateAsync(
        IReadOnlyDictionary<string, object?> values,
        CancellationToken cancellationToken);

    /// <summary>
    /// 更新动态资源记录。
    /// </summary>
    Task<IReadOnlyDictionary<string, object?>> UpdateAsync(
        string id,
        IReadOnlyDictionary<string, object?> values,
        CancellationToken cancellationToken);

    /// <summary>
    /// 删除动态资源记录。
    /// </summary>
    Task DeleteAsync(string id, CancellationToken cancellationToken);
}
