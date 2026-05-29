namespace HarborAdmin.BuildingBlocks.Abstractions.Domain;

/// <summary>软删除契约。</summary>
public interface ISoftDelete
{
    /// <summary>是否已软删除。</summary>
    bool IsDeleted { get; set; }

    /// <summary>删除时间（UTC）。</summary>
    DateTime? DeletedAt { get; set; }
}
