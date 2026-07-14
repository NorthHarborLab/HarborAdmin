namespace HarborAdmin.BuildingBlocks.Abstractions.Results;

/// <summary>
/// 稳定、可注册的业务错误定义。
/// </summary>
/// <param name="Code">全局唯一字符串错误码。</param>
/// <param name="Kind">错误分类。</param>
/// <param name="DefaultMessage">安全默认文案。</param>
/// <param name="Module">所属模块。</param>
/// <param name="Since">首次引入版本。</param>
/// <param name="ArgumentNames">允许的消息参数名。</param>
/// <param name="Retryable">是否允许调用方重试。</param>
/// <param name="Deprecated">是否已废弃。</param>
public sealed record HarborErrorDefinition(
    string Code,
    HarborErrorKind Kind,
    string DefaultMessage,
    string Module,
    string Since = "1.0",
    IReadOnlyList<string>? ArgumentNames = null,
    bool Retryable = false,
    bool Deprecated = false)
{
    /// <summary>
    /// 创建本次错误实例。
    /// </summary>
    public HarborError Create(IReadOnlyDictionary<string, object?>? arguments = null, IReadOnlyDictionary<string, string[]>? fieldErrors = null,
        object? metadata = null, string? defaultMessage = null)
    {
        var declaredArguments = ArgumentNames ?? [];
        var suppliedArguments = arguments?.Keys ?? [];
        if (!declaredArguments.ToHashSet(StringComparer.Ordinal).SetEquals(suppliedArguments))
        {
            throw new ArgumentException(
                $"Error '{Code}' arguments must be exactly [{string.Join(", ", declaredArguments)}].",
                nameof(arguments));
        }

        return new HarborError(
            Code,
            Kind,
            defaultMessage ?? DefaultMessage,
            arguments,
            fieldErrors,
            metadata,
            Retryable);
    }
}