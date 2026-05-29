namespace HarborAdmin.BuildingBlocks.Abstractions.Domain;

/// <summary>可审计实体（创建/更新时间，UTC）。</summary>
public interface IAuditable
{
    /// <summary>创建时间（UTC）。</summary>
    DateTime CreatedAt { get; set; }

    /// <summary>最后更新时间（UTC）。</summary>
    DateTime? UpdatedAt { get; set; }
}
