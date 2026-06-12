
namespace HarborAdmin.BuildingBlocks.Abstractions.Repository;

/// <summary>
/// Harbor 仓储查询选项。
/// </summary>
public sealed class HarborQueryOptions
{
    /// <summary>
    /// 排序字段。
    /// </summary>
    public string? SortField { get; init; }

    /// <summary>
    /// 排序方向。
    /// </summary>
    public string? SortOrder { get; init; }

    /// <summary>
    /// 动态筛选条件。
    /// </summary>
    public IReadOnlyList<PageFilterRule>? Filters { get; init; }

    /// <summary>
    /// 允许访问的部门 ID；空表示无数据，null 表示不限制。
    /// </summary>
    public IReadOnlySet<long>? AllowedDepartmentIds { get; init; }

    /// <summary>
    /// SQL 投影字段；空集合表示只保留主键，null 表示不限制。
    /// </summary>
    public IReadOnlySet<string>? SelectedFields { get; init; }

    /// <summary>
    /// 无额外限制的查询选项。
    /// </summary>
    public static HarborQueryOptions Empty { get; } = new();
}
