namespace HarborAdmin.BuildingBlocks.Abstractions.Domain;

/// <summary>带审计字段的 <see cref="long"/> 主键实体基类。</summary>
public abstract class AuditableEntity : EntityBase, IAuditable
{
    /// <inheritdoc />
    public DateTime CreatedAt { get; set; }

    /// <inheritdoc />
    public DateTime? UpdatedAt { get; set; }
}
