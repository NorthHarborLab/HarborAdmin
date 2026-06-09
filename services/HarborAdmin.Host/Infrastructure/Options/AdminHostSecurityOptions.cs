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
    /// 需要登录与 API 权限校验的路径前缀。
    /// </summary>
    public string[] ProtectedPathPrefixes { get; set; } =
    [
        "/api/admin",
    ];
}
