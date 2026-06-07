namespace HarborAdmin.Modules.Admin.Application.Abstractions;

/// <summary>
/// 从 access token 解析当前请求用户。
/// </summary>
public interface IAdminPrincipalResolver
{
    /// <summary>
    /// 解析 access token 并返回有效用户快照；无效或用户不可用时返回 null。
    /// </summary>
    Task<AdminPrincipalSnapshot?> ResolveAsync(string? accessToken, CancellationToken cancellationToken = default);
}
