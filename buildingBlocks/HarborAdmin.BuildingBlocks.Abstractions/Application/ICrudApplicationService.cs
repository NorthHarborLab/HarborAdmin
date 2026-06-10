namespace HarborAdmin.BuildingBlocks.Abstractions.Application;

/// <summary>
/// 基础 CRUD 应用服务契约。
/// </summary>
/// <typeparam name="TDto">输出 DTO 类型。</typeparam>
/// <typeparam name="TSaveRequest">保存请求类型。</typeparam>
public interface ICrudApplicationService<TDto, in TSaveRequest>
{
    /// <summary>
    /// 查询列表。
    /// </summary>
    Task<IReadOnlyList<TDto>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 查询详情。
    /// </summary>
    Task<TDto> GetAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 保存数据。
    /// </summary>
    Task<TDto> SaveAsync(long? id, TSaveRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除数据。
    /// </summary>
    Task DeleteAsync(long id, CancellationToken cancellationToken = default);
}
