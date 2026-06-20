using HarborAdmin.BuildingBlocks.Abstractions.Domain;

namespace HarborAdmin.BuildingBlocks.Abstractions.Repositories;

/// <summary>
/// Harbor 仓储根契约
/// </summary>
public interface IHarborRepository
{
}

/// <summary>
/// Harbor 实体仓储根契约
/// </summary>
/// <typeparam name="TEntity">实体类型</typeparam>
public interface IHarborRepository<TEntity> : IHarborRepository
    where TEntity : EntityBase
{
}
