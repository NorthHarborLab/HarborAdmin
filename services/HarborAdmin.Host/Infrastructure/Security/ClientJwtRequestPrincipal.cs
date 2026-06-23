using HarborAdmin.BuildingBlocks.Abstractions.Auth;

namespace HarborAdmin.Host.Infrastructure.Security;

/// <summary>
/// 基于当前 HTTP 请求的客户端 JWT 主体上下文。
/// </summary>
public sealed class ClientJwtRequestPrincipal : IClientJwtPrincipal
{
    /// <inheritdoc />
    public bool IsAuthenticated { get; private set; }

    /// <inheritdoc />
    public string? ProfileKey { get; private set; }

    /// <inheritdoc />
    public string? Subject { get; private set; }

    /// <inheritdoc />
    public string? JwtId { get; private set; }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string> Claims { get; private set; } =
        new Dictionary<string, string>();

    /// <summary>
    /// 写入已校验的客户端 JWT 主体。
    /// </summary>
    public void Set(
        string profileKey,
        string? subject,
        string? jwtId,
        IReadOnlyDictionary<string, string> claims)
    {
        IsAuthenticated = true;
        ProfileKey = profileKey;
        Subject = subject;
        JwtId = jwtId;
        Claims = claims;
    }
}
