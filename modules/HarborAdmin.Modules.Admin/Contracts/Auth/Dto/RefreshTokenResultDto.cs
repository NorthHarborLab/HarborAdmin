namespace HarborAdmin.Modules.Admin.Contracts.Auth.Dto;

/// <summary>
/// 刷新 token 结果。
/// </summary>
/// <param name="AccessToken">Access token。</param>
/// <param name="RefreshToken">Refresh token。</param>
/// <param name="AccessTokenExpiresAt">Access token 过期时间。</param>
/// <param name="RefreshTokenExpiresAt">Refresh token 过期时间。</param>
public sealed record RefreshTokenResultDto(string AccessToken, string RefreshToken, DateTimeOffset AccessTokenExpiresAt, DateTimeOffset RefreshTokenExpiresAt);