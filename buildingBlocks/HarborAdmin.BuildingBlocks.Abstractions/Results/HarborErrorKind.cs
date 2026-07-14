namespace HarborAdmin.BuildingBlocks.Abstractions.Results;

/// <summary>
/// 与传输协议无关的应用错误分类。
/// </summary>
public enum HarborErrorKind
{
    /// <summary>
    /// 输入或业务校验失败。
    /// </summary>
    Validation,

    /// <summary>
    /// 资源不存在。
    /// </summary>
    NotFound,

    /// <summary>
    /// 资源状态冲突。
    /// </summary>
    Conflict,

    /// <summary>
    /// 未认证。
    /// </summary>
    Unauthorized,

    /// <summary>
    /// 无权访问。
    /// </summary>
    Forbidden,

    /// <summary>
    /// 可预期业务失败。
    /// </summary>
    Business,

    /// <summary>
    /// 请求被限流。
    /// </summary>
    RateLimited,

    /// <summary>
    /// 依赖服务不可用。
    /// </summary>
    DependencyUnavailable,

    /// <summary>
    /// 操作超时。
    /// </summary>
    Timeout,
}
