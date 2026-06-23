namespace HarborAdmin.Modules.Admin.Contracts.Auth.Dto;

/// <summary>
/// 登录结果。
/// </summary>
/// <param name="AccessToken">Access token。</param>
/// <param name="RefreshToken">Refresh token。</param>
/// <param name="AccessTokenExpiresAt">Access token 过期时间。</param>
/// <param name="RefreshTokenExpiresAt">Refresh token 过期时间。</param>
public sealed record LoginResultDto(string AccessToken, string RefreshToken, DateTimeOffset AccessTokenExpiresAt, DateTimeOffset RefreshTokenExpiresAt);