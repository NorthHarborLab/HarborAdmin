namespace HarborAdmin.Modules.International.Contracts.Requests;

/// <summary>
/// 更新国际化页面请求。
/// </summary>
public sealed record UpdateInternationalPageRequest(string PageKey, string Name, string? Remark);
