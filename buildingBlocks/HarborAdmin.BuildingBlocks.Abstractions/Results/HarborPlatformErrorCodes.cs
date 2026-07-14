namespace HarborAdmin.BuildingBlocks.Abstractions.Results;

/// <summary>
/// 平台级兼容与兜底错误码。
/// </summary>
public static class HarborPlatformErrorCodes
{
    /// <summary>
    /// 通用校验失败。
    /// </summary>
    public static readonly HarborErrorDefinition Validation = new(
        "PLATFORM.REQUEST.VALIDATION_FAILED", HarborErrorKind.Validation, "请求校验失败。", "PLATFORM");

    /// <summary>
    /// 通用资源不存在。
    /// </summary>
    public static readonly HarborErrorDefinition NotFound = new(
        "PLATFORM.RESOURCE.NOT_FOUND", HarborErrorKind.NotFound, "资源不存在。", "PLATFORM");

    /// <summary>
    /// 通用资源冲突。
    /// </summary>
    public static readonly HarborErrorDefinition Conflict = new(
        "PLATFORM.RESOURCE.CONFLICT", HarborErrorKind.Conflict, "资源状态冲突。", "PLATFORM");

    /// <summary>
    /// 通用未认证。
    /// </summary>
    public static readonly HarborErrorDefinition Unauthorized = new(
        "PLATFORM.AUTH.UNAUTHORIZED", HarborErrorKind.Unauthorized, "未登录或登录已过期。", "PLATFORM");

    /// <summary>
    /// 通用禁止访问。
    /// </summary>
    public static readonly HarborErrorDefinition Forbidden = new(
        "PLATFORM.AUTH.FORBIDDEN", HarborErrorKind.Forbidden, "无权执行此操作。", "PLATFORM");

    /// <summary>
    /// 通用业务失败。
    /// </summary>
    public static readonly HarborErrorDefinition Business = new(
        "PLATFORM.BUSINESS.FAILED", HarborErrorKind.Business, "业务处理失败。", "PLATFORM");

    /// <summary>
    /// 通用内部错误。
    /// </summary>
    public static readonly HarborErrorDefinition Internal = new(
        "PLATFORM.SYSTEM.INTERNAL_ERROR", HarborErrorKind.DependencyUnavailable, "服务器内部错误，请稍后重试。", "PLATFORM", Retryable: true);
}
