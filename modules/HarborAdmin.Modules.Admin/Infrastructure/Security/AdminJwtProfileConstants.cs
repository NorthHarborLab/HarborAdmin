using HarborAdmin.BuildingBlocks.Abstractions.Auth;

namespace HarborAdmin.Modules.Admin.Infrastructure.Security;

/// <summary>
/// Admin JWT Profile 常量。
/// </summary>
public static class AdminJwtProfileConstants
{
    /// <summary>
    /// 后台管理 Profile Key。
    /// </summary>
    public const string AdminProfileKey = JwtTokenProfileKeys.Admin;

    /// <summary>
    /// 后台管理默认签名密钥引用。
    /// </summary>
    public const string AdminSigningSecretRef = "Jwt.Admin.SigningKey";

    /// <summary>
    /// 支持的 HMAC SHA-256 算法标识。
    /// </summary>
    public const string HmacSha256Algorithm = "HS256";
}
