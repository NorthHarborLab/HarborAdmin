namespace HarborAdmin.Modules.Admin.Contracts.Auth.Request;

/// <summary>
/// 登出请求。
/// </summary>
public sealed class LogoutRequest
{
    /// <summary>
    /// Refresh token；为空时读取 HttpOnly Cookie。
    /// </summary>
    public string? RefreshToken { get; set; }
}
