using HarborAdmin.BuildingBlocks.Abstractions.Domain;
using HarborAdmin.BuildingBlocks.Abstractions.Enums;
using HarborAdmin.BuildingBlocks.Abstractions.Exception;
using HarborAdmin.BuildingBlocks.Abstractions.Repositories;
using HarborAdmin.BuildingBlocks.Abstractions.Repositories.Models;

namespace HarborAdmin.BuildingBlocks.Abstractions.Application;

/// <summary>
/// Harbor Repository 驱动的基础 CRUD 应用服务。
/// </summary>
/// <typeparam name="TEntity">实体类型。</typeparam>
/// <typeparam name="TDto">输出 DTO 类型。</typeparam>
/// <typeparam name="TSaveRequest">保存请求类型。</typeparam>
/// <typeparam name="TRepository">实体仓储类型。</typeparam>
public abstract class HarborApplicationRepositoryService<TEntity, TDto, TSaveRequest, TRepository>
    : HarborApplicationService, ICrudApplicationService<TDto, TSaveRequest>
    where TEntity : EntityBase, new()
    where TRepository : IHarborCrudRepository<TEntity>
{
    /// <summary>
    /// 初始化 Repository 驱动的 CRUD 应用服务。
    /// </summary>
    /// <param name="repository">实体仓储。</param>
    protected HarborApplicationRepositoryService(TRepository repository)
    {
        Repository = repository;
    }

    /// <summary>
    /// 实体仓储。
    /// </summary>
    protected TRepository Repository { get; }

    /// <inheritdoc />
    public virtual async Task<IReadOnlyList<TDto>> ListAsync(CancellationToken cancellationToken = default) =>
        (await Repository.ListAsync(HarborQueryOptions.Empty, cancellationToken))
        .Select(MapToDto)
        .ToList();

    /// <inheritdoc />
    public virtual async Task<TDto> GetAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = RequireFound(await Repository.GetAsync(id, cancellationToken), GetNotFoundMessage(id));
        return MapToDto(entity);
    }

    /// <inheritdoc />
    public virtual async Task<TDto> SaveAsync(long? id, TSaveRequest request, CancellationToken cancellationToken = default)
    {
        var isUpdate = id is > 0;
        var entity = isUpdate
            ? RequireFound(await Repository.GetAsync(id!.Value, cancellationToken), GetNotFoundMessage(id.Value))
            : CreateEntity(request);

        if (isUpdate)
        {
            await ValidateUpdateAsync(entity, request, cancellationToken);
        }
        else
        {
            await ValidateCreateAsync(entity, request, cancellationToken);
        }

        await ApplySaveAsync(entity, request, cancellationToken);
        var saved = isUpdate
            ? await Repository.UpdateAsync(entity, cancellationToken)
            : await Repository.InsertAsync(entity, cancellationToken);
        await AfterSaveAsync(saved, request, cancellationToken);
        return MapToDto(saved);
    }

    /// <inheritdoc />
    public virtual async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = RequireFound(await Repository.GetAsync(id, cancellationToken), GetNotFoundMessage(id));
        var decision = await CanDeleteAsync(entity, cancellationToken);
        switch (decision)
        {
            case CrudDeleteDecision.PhysicalDelete:
                await Repository.DeleteAsync(id, cancellationToken);
                break;
            case CrudDeleteDecision.SoftDelete:
                await Repository.SoftDeleteAsync(entity, cancellationToken);
                break;
            case CrudDeleteDecision.Reject:
                throw new ConflictDomainException(GetDeleteRejectedMessage(entity));
            default:
                throw new ValidationDomainException($"Unsupported delete decision '{decision}'.");
        }

        await AfterDeleteAsync(entity, decision, cancellationToken);
    }

    /// <summary>
    /// 创建新实体。
    /// </summary>
    /// <param name="request">保存请求。</param>
    /// <returns>新实体。</returns>
    protected virtual TEntity CreateEntity(TSaveRequest request) => new();

    /// <summary>
    /// 映射实体到 DTO。
    /// </summary>
    /// <param name="entity">实体。</param>
    /// <returns>DTO。</returns>
    protected abstract TDto MapToDto(TEntity entity);

    /// <summary>
    /// 将保存请求应用到实体。
    /// </summary>
    /// <param name="entity">实体。</param>
    /// <param name="request">保存请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    protected abstract Task ApplySaveAsync(TEntity entity, TSaveRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// 创建前校验。
    /// </summary>
    protected virtual Task ValidateCreateAsync(TEntity entity, TSaveRequest request, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    /// <summary>
    /// 更新前校验。
    /// </summary>
    protected virtual Task ValidateUpdateAsync(TEntity entity, TSaveRequest request, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    /// <summary>
    /// 判断是否可删除。
    /// </summary>
    protected virtual Task<CrudDeleteDecision> CanDeleteAsync(TEntity entity, CancellationToken cancellationToken) =>
        Task.FromResult(CrudDeleteDecision.PhysicalDelete);

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

    /// <summary>
    /// 构造未找到消息。
    /// </summary>
    protected virtual string GetNotFoundMessage(long id) => $"{typeof(TEntity).Name} '{id}' was not found.";

    /// <summary>
    /// 构造拒绝删除消息。
    /// </summary>
    protected virtual string GetDeleteRejectedMessage(TEntity entity) => $"{typeof(TEntity).Name} '{entity.Id}' cannot be deleted.";
}
