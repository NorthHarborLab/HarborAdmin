namespace HarborAdmin.Host.Infrastructure.Options;

/// <summary>
/// Admin Host 安全管道配置。
/// </summary>
public sealed class AdminHostSecurityOptions
{
    /// <summary>
    /// 配置节名称。
    /// </summary>
    public const string SectionName = "Harbor:AdminHostSecurity";

    /// <summary>
    /// 匿名可访问的路径前缀。
    /// </summary>
    public string[] PublicPathPrefixes { get; set; } =
    [
        "/api/auth/crypto-challenge",
        "/api/auth/captcha",
        "/api/auth/login",
        "/api/auth/refresh",
        "/openapi",
        "/admin/international/resources",
    ];

    /// <summary>
    /// 需要登录与 API 权限校验的路径前缀。
    /// </summary>
    public string[] ProtectedPathPrefixes { get; set; } =
    [
        "/api/admin/system",
        "/api/admin/access",
        "/api/admin/feature-design",
        "/api/admin/features",
        "/api/admin/dynamic-crud",
    ];
}
