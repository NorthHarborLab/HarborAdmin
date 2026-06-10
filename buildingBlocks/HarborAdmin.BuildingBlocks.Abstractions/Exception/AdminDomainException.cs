using System.Collections.ObjectModel;
using HarborAdmin.BuildingBlocks.Abstractions.ModelResults;

namespace HarborAdmin.BuildingBlocks.Abstractions.Exception;

/// <summary>
/// 统一业务异常基类，携带业务码与HTTP状态码。
/// </summary>
public abstract class AdminDomainException : System.Exception
{
    /// <summary>
    /// 业务错误码。
    /// </summary>
    public int Code { get; }

    /// <summary>
    /// 建议返回的HTTP状态码。
    /// </summary>
    public int HttpStatus { get; }

    /// <summary>
    /// 字段级错误明细。
    /// </summary>
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    /// <summary>
    /// 额外错误元数据。
    /// </summary>
    public object? ErrorMeta { get; }

    /// <summary>
    /// 初始化领域异常。
    /// </summary>
    protected AdminDomainException(
        int code,
        string message,
        int httpStatus,
        IReadOnlyDictionary<string, string[]>? errors = null,
        object? errorMeta = null,
        System.Exception? innerException = null)
        : base(message, innerException)
    {
        Code = code;
        HttpStatus = httpStatus;
        Errors = errors is null
            ? new ReadOnlyDictionary<string, string[]>(new Dictionary<string, string[]>())
            : errors;
        ErrorMeta = errorMeta;
    }
}

/// <summary>
/// 参数校验失败异常。
/// </summary>
public sealed class ValidationDomainException : AdminDomainException
{
    /// <summary>
    /// 初始化参数校验失败异常。
    /// </summary>
    /// <param name="message">错误描述。</param>
    /// <param name="errors">字段级错误明细。</param>
    /// <param name="errorMeta">额外错误元数据。</param>
    /// <param name="innerException">内部异常。</param>
    public ValidationDomainException(
        string message,
        IReadOnlyDictionary<string, string[]>? errors = null,
        object? errorMeta = null,
        System.Exception? innerException = null)
        : base(ApiResultCodes.BadRequest, message, 400, errors, errorMeta, innerException)
    {
    }
}

/// <summary>
/// 资源未找到异常。
/// </summary>
public sealed class NotFoundDomainException : AdminDomainException
{
    /// <summary>
    /// 初始化资源未找到异常。
    /// </summary>
    /// <param name="message">错误描述。</param>
    /// <param name="errorMeta">额外错误元数据。</param>
    /// <param name="innerException">内部异常。</param>
    public NotFoundDomainException(
        string message,
        object? errorMeta = null,
        System.Exception? innerException = null)
        : base(ApiResultCodes.NotFound, message, 404, null, errorMeta, innerException)
    {
    }
}

/// <summary>
/// 资源冲突异常。
/// </summary>
public sealed class ConflictDomainException : AdminDomainException
{
    /// <summary>
    /// 初始化资源冲突异常。
    /// </summary>
    /// <param name="message">错误描述。</param>
    /// <param name="errorMeta">额外错误元数据。</param>
    /// <param name="innerException">内部异常。</param>
    public ConflictDomainException(
        string message,
        object? errorMeta = null,
        System.Exception? innerException = null)
        : base(ApiResultCodes.Conflict, message, 409, null, errorMeta, innerException)
    {
    }
}

/// <summary>
/// 禁止访问异常。
/// </summary>
public sealed class ForbiddenDomainException : AdminDomainException
{
    /// <summary>
    /// 初始化禁止访问异常。
    /// </summary>
    /// <param name="message">错误描述。</param>
    /// <param name="errorMeta">额外错误元数据。</param>
    /// <param name="innerException">内部异常。</param>
    public ForbiddenDomainException(
        string message,
        object? errorMeta = null,
        System.Exception? innerException = null)
        : base(ApiResultCodes.Forbidden, message, 403, null, errorMeta, innerException)
    {
    }
}

/// <summary>
/// 未授权异常。
/// </summary>
public sealed class UnauthorizedDomainException : AdminDomainException
{
    /// <summary>
    /// 初始化未授权异常。
    /// </summary>
    /// <param name="message">错误描述。</param>
    /// <param name="errorMeta">额外错误元数据。</param>
    /// <param name="innerException">内部异常。</param>
    public UnauthorizedDomainException(
        string message,
        object? errorMeta = null,
        System.Exception? innerException = null)
        : base(ApiResultCodes.Unauthorized, message, 401, null, errorMeta, innerException)
    {
    }
}

/// <summary>
/// 自定义业务码异常，适用于需要返回特定业务错误码的场景。
/// </summary>
public sealed class BusinessDomainException : AdminDomainException
{
    /// <summary>
    /// 初始化自定义业务码异常。
    /// </summary>
    /// <param name="code">业务错误码。</param>
    /// <param name="message">错误描述。</param>
    /// <param name="httpStatus">建议返回的HTTP状态码。</param>
    /// <param name="errors">字段级错误明细。</param>
    /// <param name="errorMeta">额外错误元数据。</param>
    /// <param name="innerException">内部异常。</param>
    public BusinessDomainException(
        int code,
        string message,
        int httpStatus = 400,
        IReadOnlyDictionary<string, string[]>? errors = null,
        object? errorMeta = null,
        System.Exception? innerException = null)
        : base(code, message, httpStatus, errors, errorMeta, innerException)
    {
    }
}
