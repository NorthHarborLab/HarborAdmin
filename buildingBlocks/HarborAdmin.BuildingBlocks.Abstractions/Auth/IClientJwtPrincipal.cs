namespace HarborAdmin.BuildingBlocks.Abstractions.Auth;

/// <summary>
/// 客户端 JWT 主体上下文。
/// </summary>
public interface IClientJwtPrincipal
{
    /// <summary>
    /// 是否已通过客户端 JWT 校验。
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// JWT Profile Key。
    /// </summary>
    string? ProfileKey { get; }

    /// <summary>
    /// JWT Subject。
    /// </summary>
    string? Subject { get; }

    /// <summary>
    /// JWT ID。
    /// </summary>
    string? JwtId { get; }

    /// <summary>
    /// JWT 声明。
    /// </summary>
    IReadOnlyDictionary<string, string> Claims { get; }
}
