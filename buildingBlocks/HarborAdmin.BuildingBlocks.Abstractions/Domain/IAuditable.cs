namespace HarborAdmin.BuildingBlocks.Abstractions.Domain;

/// <summary>
/// 可审计实体。
/// </summary>
public interface IAuditable
{
    /// <summary>
    /// 创建时间（UTC）
    /// </summary>
    DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// 最后更新时间（UTC）
    /// </summary>
    DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>
    /// 创建人用户主键；系统或未登录上下文为 0。
    /// </summary>
    long CreatedBy { get; set; }

    /// <summary>
    /// 最后更新人用户主键；系统或未登录上下文为 0。
    /// </summary>
    long UpdatedBy { get; set; }
}
