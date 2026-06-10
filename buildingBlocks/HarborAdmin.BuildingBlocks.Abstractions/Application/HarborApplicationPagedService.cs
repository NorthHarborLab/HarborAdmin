using HarborAdmin.BuildingBlocks.Abstractions.ModelResults;

namespace HarborAdmin.BuildingBlocks.Abstractions.Application;

/// <summary>
/// Harbor 基础分页 CRUD 应用服务。
/// </summary>
/// <typeparam name="TDto">输出 DTO 类型。</typeparam>
/// <typeparam name="TQuery">分页查询请求类型。</typeparam>
/// <typeparam name="TSaveRequest">保存请求类型。</typeparam>
public abstract class HarborApplicationPagedService<TDto, TQuery, TSaveRequest> : HarborApplicationService<TDto, TSaveRequest>,
    IPagedCrudApplicationService<TDto, TQuery, TSaveRequest>
    where TQuery : PageRequest
{
    /// <inheritdoc />
    public abstract Task<PagedResult<TDto>> PageAsync(TQuery query, CancellationToken cancellationToken = default);
}
