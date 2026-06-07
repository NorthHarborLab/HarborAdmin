namespace HarborAdmin.Modules.Admin.Infrastructure.Security;

/// <summary>
/// Access token 载荷。
/// </summary>
/// <param name="UserId">用户 ID。</param>
/// <param name="UserName">登录名。</param>
/// <param name="ExpiresAt">过期时间（Unix 秒）。</param>
public sealed record TokenPayload(long UserId, string UserName, long ExpiresAt);
