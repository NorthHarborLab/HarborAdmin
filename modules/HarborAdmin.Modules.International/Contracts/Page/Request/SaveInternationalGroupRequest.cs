namespace HarborAdmin.Modules.International.Contracts.Page.Request;

/// <summary>
/// 保存国际化资源分组请求。
/// </summary>
public sealed record SaveInternationalGroupRequest(
    long? ParentId,
    string Key,
    string Name,
    int SortOrder);
