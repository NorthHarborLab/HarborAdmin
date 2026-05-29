namespace HarborAdmin.BuildingBlocks.Abstractions.Domain;

/// <summary>以 <see cref="long"/> 为主键的实体基类。</summary>
public abstract class EntityBase : IEntity<long>
{
    /// <inheritdoc />
    public long Id { get; set; }
}
