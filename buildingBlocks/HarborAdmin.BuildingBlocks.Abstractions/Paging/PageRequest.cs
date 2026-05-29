namespace HarborAdmin.BuildingBlocks.Abstractions.Paging;

/// <summary>分页查询请求（与前端 vxe-grid 页码约定一致）。</summary>
public class PageRequest
{
    /// <summary>单页最大条数。</summary>
    public const int MaxPageSize = 200;

    /// <summary>默认每页条数。</summary>
    public const int DefaultPageSize = 20;

    private int _page = 1;
    private int _pageSize = DefaultPageSize;

    /// <summary>页码，从 1 开始。</summary>
    public int Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }

    /// <summary>每页条数。</summary>
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

    /// <summary>跳过的记录数。</summary>
    public int Skip => (Page - 1) * PageSize;
}
