namespace HarborAdmin.BuildingBlocks.Abstractions.Api;

/// <summary>
/// 无数据负载的统一 API 响应
/// </summary>
public sealed class ApiResult
{
    /// <summary>
    /// 业务状态码，<c>0</c> 表示成功
    /// </summary>
    public int Code { get; init; }

    /// <summary>
    /// 提示信息
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// 创建成功响应
    /// </summary>
    public static ApiResult Ok(string? message = null) =>
        new() { Code = ApiResultCodes.Success, Message = message };

    /// <summary>
    /// 创建成功响应（泛型）
    /// </summary>
    public static ApiResult<T> Ok<T>(T data, string? message = null) =>
        new() { Code = ApiResultCodes.Success, Data = data, Message = message };

    /// <summary>
    /// 创建失败响应
    /// </summary>
    public static ApiResult Fail(int code, string message) =>
        new() { Code = code, Message = message };

    /// <summary>
    /// 创建失败响应（泛型）
    /// </summary>
    public static ApiResult<T> Fail<T>(int code, string message) =>
        new() { Code = code, Message = message };
}

/// <summary>
/// 带数据负载的统一 API 响应
/// </summary>
/// <typeparam name="T">业务数据类型</typeparam>
public sealed class ApiResult<T>
{
    /// <summary>
    /// 业务状态码，<c>0</c> 表示成功
    /// </summary>
    public int Code { get; init; }

    /// <summary>
    /// 提示信息
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// 业务数据
    /// </summary>
    public T? Data { get; init; }

    /// <summary>
    /// 创建成功响应
    /// </summary>
    public static ApiResult<T> Ok(T data, string? message = null) =>
        new() { Code = ApiResultCodes.Success, Data = data, Message = message };

    /// <summary>
    /// 创建失败响应
    /// </summary>
    public static ApiResult<T> Fail(int code, string message) =>
        new() { Code = code, Message = message };
}
