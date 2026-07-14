using HarborAdmin.BuildingBlocks.Abstractions.Application;
using HarborAdmin.BuildingBlocks.Abstractions.Domain;
using HarborAdmin.BuildingBlocks.Abstractions.Enums;
using HarborAdmin.BuildingBlocks.Abstractions.ModelResults;
using HarborAdmin.BuildingBlocks.Abstractions.Repositories;
using HarborAdmin.BuildingBlocks.Abstractions.Results;

namespace HarborAdmin.BuildingBlocks.Application;

/// <summary>
/// Harbor Repository 驱动的 CRUD 应用服务。
/// </summary>
/// <typeparam name="TEntity">实体类型。</typeparam>
/// <typeparam name="TDto">输出 DTO 类型。</typeparam>
/// <typeparam name="TQuery">分页查询请求类型。</typeparam>
/// <typeparam name="TSaveRequest">保存请求类型。</typeparam>
/// <typeparam name="TRepository">实体仓储类型。</typeparam>
public abstract class HarborCrudApplicationService<TEntity, TDto, TQuery, TSaveRequest, TRepository>
    : HarborQueryApplicationService<TEntity, TDto, TQuery, TRepository>, IHarborCrudApplicationService<TDto, TQuery, TSaveRequest>
    where TEntity : EntityBase, new()
    where TQuery : PageRequest
    where TRepository : IHarborCrudRepository<TEntity>
{
    /// <summary>
    /// 初始化 Repository 驱动的 CRUD 应用服务。
    /// </summary>
    /// <param name="repository">实体仓储。</param>
    protected HarborCrudApplicationService(TRepository repository) : base(repository)
    {
    }

    /// <inheritdoc />
    public virtual async Task<HarborResult<TDto>> SaveAsync(long? id, TSaveRequest request, CancellationToken cancellationToken)
    {
        var isUpdate = id is > 0;
        var entity = isUpdate ? await Repository.GetAsync(id!.Value, cancellationToken) : CreateEntity(request);
        if (entity is null)
        {
            return HarborResult<TDto>.Failure(CreateNotFoundError(id!.Value));
        }

        var validation = isUpdate
            ? await ValidateUpdateAsync(entity, request, cancellationToken)
            : await ValidateCreateAsync(entity, request, cancellationToken);
        if (!validation.IsSuccess)
        {
            return HarborResult<TDto>.Failure(validation.Error!);
        }

        var apply = await ApplySaveAsync(entity, request, cancellationToken);
        if (!apply.IsSuccess)
        {
            return HarborResult<TDto>.Failure(apply.Error!);
        }

        var saved = isUpdate
            ? await Repository.UpdateAsync(entity, cancellationToken)
            : await Repository.InsertAsync(entity, cancellationToken);
        await AfterSaveAsync(saved, request, cancellationToken);
        return HarborResult<TDto>.Success(MapToDto(saved));
    }

    /// <inheritdoc />
    public virtual async Task<HarborResult<bool>> DeleteAsync(long id, CancellationToken cancellationToken)
    {
        var entity = await Repository.GetAsync(id, cancellationToken);
        if (entity is null)
        {
            return HarborResult<bool>.Failure(CreateNotFoundError(id));
        }

        var decision = await CanDeleteAsync(entity, cancellationToken);
        if (!decision.IsSuccess)
        {
            return HarborResult<bool>.Failure(decision.Error!);
        }

        switch (decision.Value)
        {
            case CrudDeleteDecision.PhysicalDelete:
                await Repository.DeleteAsync(id, cancellationToken);
                break;
            case CrudDeleteDecision.SoftDelete:
                await Repository.SoftDeleteAsync(entity, cancellationToken);
                break;
            default:
                throw new InvalidOperationException($"Unsupported delete decision '{decision.Value}'.");
        }

        await AfterDeleteAsync(entity, decision.Value, cancellationToken);
        return HarborResult<bool>.Success(true);
    }

    /// <summary>
    /// 创建新实体。
    /// </summary>
    /// <param name="request">保存请求。</param>
    /// <returns>新实体。</returns>
    protected virtual TEntity CreateEntity(TSaveRequest request) => new();

    /// <summary>
    /// 将保存请求应用到实体。
    /// </summary>
    /// <param name="entity">实体。</param>
    /// <param name="request">保存请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    protected abstract Task<HarborResult> ApplySaveAsync(TEntity entity, TSaveRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// 创建前校验。
    /// </summary>
    protected virtual Task<HarborResult> ValidateCreateAsync(TEntity entity, TSaveRequest request, CancellationToken cancellationToken) =>
        Task.FromResult(HarborResult.Success());

    /// <summary>
    /// 更新前校验。
    /// </summary>
    protected virtual Task<HarborResult> ValidateUpdateAsync(TEntity entity, TSaveRequest request, CancellationToken cancellationToken) =>
        Task.FromResult(HarborResult.Success());

    /// <summary>
    /// 判断是否可删除。
    /// </summary>
    protected virtual Task<HarborResult<CrudDeleteDecision>> CanDeleteAsync(TEntity entity, CancellationToken cancellationToken) =>
        Task.FromResult(HarborResult<CrudDeleteDecision>.Success(CrudDeleteDecision.PhysicalDelete));

    /// <summary>
    /// 保存后处理。
    /// </summary>
    protected virtual Task AfterSaveAsync(TEntity entity, TSaveRequest request, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    /// <summary>
    /// 删除后处理。
    /// </summary>
    protected virtual Task AfterDeleteAsync(TEntity entity, CrudDeleteDecision decision, CancellationToken cancellationToken) =>
        Task.CompletedTask;

}
