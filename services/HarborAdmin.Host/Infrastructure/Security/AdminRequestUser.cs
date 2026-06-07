using HarborAdmin.BuildingBlocks.Abstractions.Auth;

namespace HarborAdmin.Host.Infrastructure.Security;

/// <summary>
/// 基于当前 HTTP 请求的用户上下文。
/// </summary>
public sealed class AdminRequestUser : ICurrentUser
{
    /// <summary>
    /// 用户 ID。
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 登录名。
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// 显示名。
    /// </summary>
    public string? DisplayName { get; set; }
}
