namespace HarborAdmin.BuildingBlocks.Abstractions.ModelResults;

/// <summary>
/// 分页查询结果
/// </summary>
/// <typeparam name="T">项类型。</typeparam>
public sealed class PagedResult<T>
{
    /// <summary>
    /// 当前页数据
    /// </summary>
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();

    /// <summary>
    /// 总记录数
    /// </summary>
    public long Total { get; init; }

    /// <summary>
    /// 从列表构建分页结果
    /// </summary>
    public static PagedResult<T> From(IReadOnlyList<T> items, long total) =>
        new() { Items = items, Total = total };
    
    /// <summary>
    /// 从列表构建分页结果
    /// </summary>
    public static PagedResult<T> Ok(IReadOnlyList<T> items, long total) =>
        new() { Items = items, Total = total };
}