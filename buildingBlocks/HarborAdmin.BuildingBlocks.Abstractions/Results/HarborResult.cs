namespace HarborAdmin.BuildingBlocks.Abstractions.Results;

/// <summary>
/// 无数据应用处理结果。
/// </summary>
public sealed class HarborResult
{
    private HarborResult(HarborError? error)
    {
        Error = error;
    }

    /// <summary>
    /// 是否成功。
    /// </summary>
    public bool IsSuccess => Error is null;

    /// <summary>
    /// 失败信息。
    /// </summary>
    public HarborError? Error { get; }

    /// <summary>
    /// 创建成功结果。
    /// </summary>
    public static HarborResult Success() => new(null);

    /// <summary>
    /// 创建失败结果。
    /// </summary>
    public static HarborResult Failure(HarborError error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new HarborResult(error);
    }
}

/// <summary>
/// 带数据应用处理结果。
/// </summary>
/// <typeparam name="T">数据类型。</typeparam>
public sealed class HarborResult<T>
{
    private HarborResult(T? value, HarborError? error)
    {
        Value = value;
        Error = error;
    }

    /// <summary>
    /// 是否成功。
    /// </summary>
    public bool IsSuccess => Error is null;

    /// <summary>
    /// 成功数据。
    /// </summary>
    public T? Value { get; }

    /// <summary>
    /// 失败信息。
    /// </summary>
    public HarborError? Error { get; }

    /// <summary>
    /// 创建成功结果。
    /// </summary>
    public static HarborResult<T> Success(T value) => new(value, null);

    /// <summary>
    /// 创建失败结果。
    /// </summary>
    public static HarborResult<T> Failure(HarborError error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new HarborResult<T>(default, error);
    }
}
