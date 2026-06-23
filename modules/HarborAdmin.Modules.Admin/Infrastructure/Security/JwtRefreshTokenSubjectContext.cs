namespace HarborAdmin.Modules.Admin.Infrastructure.Security;

/// <summary>
/// 刷新令牌主体上下文。
/// </summary>
/// <param name="ProfileKey">JWT Profile Key。</param>
/// <param name="Subject">主体标识。</param>
/// <param name="SubjectType">主体类型。</param>
public sealed record JwtRefreshTokenSubjectContext(
    string ProfileKey,
    string Subject,
    string SubjectType);
