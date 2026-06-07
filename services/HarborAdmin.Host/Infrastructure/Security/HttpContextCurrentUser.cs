using HarborAdmin.BuildingBlocks.Abstractions.Auth;

namespace HarborAdmin.Host.Infrastructure.Security;

/// <summary>
/// 基于当前 HTTP 请求作用域解析 <see cref="AdminRequestUser"/> 的用户上下文。
/// </summary>
/// <remarks>
/// 注册为 Singleton，供 FreeSql 审计 AOP 等根容器解析场景使用；实际用户数据来自请求 Scoped 实例。
/// </remarks>
public sealed class HttpContextCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    /// <summary>
    /// 当前请求的用户上下文。
    /// </summary>
    private AdminRequestUser? RequestUser =>
        httpContextAccessor.HttpContext?.RequestServices.GetService<AdminRequestUser>();

    /// <inheritdoc />
    public long Id => RequestUser?.Id ?? 0;

    /// <inheritdoc />
    public string? UserName => RequestUser?.UserName;

    /// <inheritdoc />
    public string? DisplayName => RequestUser?.DisplayName;
}
