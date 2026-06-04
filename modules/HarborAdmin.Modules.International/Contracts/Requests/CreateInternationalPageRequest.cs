namespace HarborAdmin.Modules.International.Contracts.Requests;

/// <summary>
/// 创建国际化页面请求。
/// </summary>
public sealed record CreateInternationalPageRequest(
    string PageKey,
    string Name,
    string? Remark);
