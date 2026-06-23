namespace HarborAdmin.Modules.Admin.Contracts.Auth.Request;

/// <summary>
/// 刷新 token 请求。
/// </summary>
public sealed class RefreshTokenRequest
{
    /// <summary>
    /// Refresh token；为空时读取 HttpOnly Cookie。
    /// </summary>
    public string? RefreshToken { get; set; }
}
