using HarborAdmin.BuildingBlocks.Abstractions.ModelResults;
using HarborAdmin.BuildingBlocks.Abstractions.Results;

namespace HarborAdmin.BuildingBlocks.Abstractions.Application;

/// <summary>
/// Harbor CRUD 应用服务契约。
/// </summary>
/// <typeparam name="TDto">输出 DTO 类型。</typeparam>
/// <typeparam name="TQuery">分页查询请求类型。</typeparam>
/// <typeparam name="TSaveRequest">保存请求类型。</typeparam>
public interface IHarborCrudApplicationService<TDto, in TQuery, in TSaveRequest> : IHarborQueryApplicationService<TDto, TQuery>
    where TQuery : PageRequest
{
    /// <summary>
    /// 保存数据。
    /// </summary>
    Task<HarborResult<TDto>> SaveAsync(long? id, TSaveRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// 删除数据。
    /// </summary>
    Task<HarborResult<bool>> DeleteAsync(long id, CancellationToken cancellationToken);
}
