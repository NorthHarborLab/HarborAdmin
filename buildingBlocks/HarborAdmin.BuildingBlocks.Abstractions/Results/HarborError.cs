namespace HarborAdmin.BuildingBlocks.Abstractions.Results;

/// <summary>
/// 单次应用失败信息。
/// </summary>
/// <param name="Code">稳定业务错误码。</param>
/// <param name="Kind">错误分类。</param>
/// <param name="DefaultMessage">安全默认文案。</param>
/// <param name="Arguments">i18n 参数。</param>
/// <param name="FieldErrors">字段错误。</param>
/// <param name="Metadata">附加元数据。</param>
/// <param name="Retryable">是否允许重试。</param>
public sealed record HarborError(
    string Code,
    HarborErrorKind Kind,
    string DefaultMessage,
    IReadOnlyDictionary<string, object?>? Arguments = null,
    IReadOnlyDictionary<string, string[]>? FieldErrors = null,
    object? Metadata = null,
    bool Retryable = false);