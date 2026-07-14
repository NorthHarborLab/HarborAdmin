using HarborAdmin.BuildingBlocks.Abstractions.Results;

namespace HarborAdmin.Modules.AI.Contracts.Shared.ErrorCode;

/// <summary>
/// AI 供应商错误码。
/// </summary>
public static class AiProviderErrorCodes
{
    /// <summary>
    /// 供应商不存在。
    /// </summary>
    public static readonly HarborErrorDefinition NotFound = new(
        "AI.PROVIDER.NOT_FOUND", HarborErrorKind.NotFound, "AI 供应商不存在。", "AI", ArgumentNames: ["id"]);

    /// <summary>
    /// 供应商输入无效。
    /// </summary>
    public static readonly HarborErrorDefinition InvalidInput = new(
        "AI.PROVIDER.INVALID_INPUT", HarborErrorKind.Validation, "AI 供应商输入无效。", "AI", ArgumentNames: ["reason"]);

    /// <summary>
    /// 密钥引用不可用。
    /// </summary>
    public static readonly HarborErrorDefinition SecretUnavailable = new(
        "AI.PROVIDER.SECRET_UNAVAILABLE", HarborErrorKind.Validation, "供应商密钥引用不存在或未启用。", "AI", ArgumentNames: ["secretRef"]);

    /// <summary>
    /// 缺少供应商模型。
    /// </summary>
    public static readonly HarborErrorDefinition ModelRequired = new(
        "AI.PROVIDER.MODEL_REQUIRED", HarborErrorKind.Validation, "至少需要配置一个供应商模型。", "AI");

    /// <summary>
    /// 供应商 Key 已存在。
    /// </summary>
    public static readonly HarborErrorDefinition DuplicateKey = new(
        "AI.PROVIDER.DUPLICATE_KEY", HarborErrorKind.Conflict, "AI 供应商 Key 已存在。", "AI", ArgumentNames: ["providerKey"]);
}
