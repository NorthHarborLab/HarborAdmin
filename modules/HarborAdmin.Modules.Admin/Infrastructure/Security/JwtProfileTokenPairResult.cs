namespace HarborAdmin.Modules.Admin.Infrastructure.Security;

/// <summary>
/// JWT Profile token pair 签发结果。
/// </summary>
/// <param name="ProfileKey">JWT Profile Key。</param>
/// <param name="Subject">主体标识。</param>
/// <param name="AccessToken">Access token。</param>
/// <param name="RefreshToken">Refresh token。</param>
/// <param name="AccessTokenExpiresAt">Access token 过期时间。</param>
/// <param name="RefreshTokenExpiresAt">Refresh token 过期时间。</param>
public sealed record JwtProfileTokenPairResult(
    string ProfileKey,
    string Subject,
    string AccessToken,
    string RefreshToken,
    DateTimeOffset AccessTokenExpiresAt,
    DateTimeOffset RefreshTokenExpiresAt);
