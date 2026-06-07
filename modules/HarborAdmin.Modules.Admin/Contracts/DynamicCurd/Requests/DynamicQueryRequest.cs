namespace HarborAdmin.Modules.Admin.Contracts.DynamicCurd.Requests;

/// <summary>
/// 动态 CRUD 查询请求。
/// </summary>
public sealed record DynamicQueryRequest(
    int Page,
    int PageSize,
    IReadOnlyDictionary<string, object?>? Search,
    IReadOnlyList<DynamicSortItem>? Sorts);

/// <summary>
/// 动态 CRUD 排序项。
/// </summary>
public sealed record DynamicSortItem(
    string Field,
    string? Order);
