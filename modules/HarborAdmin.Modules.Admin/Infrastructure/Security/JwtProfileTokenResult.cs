namespace HarborAdmin.Modules.Admin.Infrastructure.Security;

/// <summary>
/// JWT Profile token 校验结果。
/// </summary>
public sealed record JwtProfileTokenResult(
    string ProfileKey,
    string? Subject,
    string? JwtId,
    IReadOnlyDictionary<string, string> Claims);
