namespace HarborAdmin.BuildingBlocks.Abstractions.Repositories.Models;

/// <summary>
/// Harbor 仓储查询选项
/// </summary>
public class HarborQueryOptions
{
    /// <summary>
    /// 单页最大条数
    /// </summary>
    public const int MaxPageSize = 200;

    /// <summary>
    /// 默认每页条数
    /// </summary>
    public const int DefaultPageSize = 20;

    private int _page = 1;
    private int _pageSize = DefaultPageSize;

    /// <summary>
    /// 页码，从 1 开始
    /// </summary>
    public int Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }

    /// <summary>
    /// 每页条数
    /// </summary>
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value switch
        {
            < 1 => DefaultPageSize,
            > MaxPageSize => MaxPageSize,
            _ => value,
        };
    }

    /// <summary>
    /// 跳过的记录数
    /// </summary>
    public int Skip => (Page - 1) * PageSize;

    /// <summary>
    /// 排序字段
    /// </summary>
    public string? SortField { get; set; }

    /// <summary>
    /// 排序方向
    /// </summary>
    public string? SortOrder { get; set; }

    /// <summary>
    /// 动态筛选条件
    /// </summary>
    public IReadOnlyList<PageFilterRule>? Filters { get; set; }

    /// <summary>
    /// 允许访问的部门 ID；空表示无数据，null 表示不限制
    /// </summary>
    public IReadOnlySet<long>? AllowedDepartmentIds { get; set; }

    /// <summary>
    /// SQL 投影字段；空集合表示只保留主键，null 表示不限制
    /// </summary>
    public IReadOnlySet<string>? SelectedFields { get; set; }

    /// <summary>
    /// 无额外限制的查询选项
    /// </summary>
    public static HarborQueryOptions Empty => new();
}
