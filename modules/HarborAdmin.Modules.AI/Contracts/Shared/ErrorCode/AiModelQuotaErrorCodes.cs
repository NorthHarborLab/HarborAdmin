using HarborAdmin.BuildingBlocks.Abstractions.Results;

namespace HarborAdmin.Modules.AI.Contracts.Shared.ErrorCode;

/// <summary>
/// AI 模型限额错误码。
/// </summary>
public static class AiModelQuotaErrorCodes
{
    /// <summary>
    /// 模型限额不存在。
    /// </summary>
    public static readonly HarborErrorDefinition NotFound = new(
        "AI.MODEL_QUOTA.NOT_FOUND", HarborErrorKind.NotFound, "AI 模型限额不存在。", "AI", ArgumentNames: ["id"]);

    /// <summary>
    /// 模型限额输入无效。
    /// </summary>
    public static readonly HarborErrorDefinition InvalidInput = new(
        "AI.MODEL_QUOTA.INVALID_INPUT", HarborErrorKind.Validation, "AI 模型限额输入无效。", "AI", ArgumentNames: ["reason"]);

    /// <summary>
    /// 模型限额作用域已存在。
    /// </summary>
    public static readonly HarborErrorDefinition DuplicateScope = new(
        "AI.MODEL_QUOTA.DUPLICATE_SCOPE", HarborErrorKind.Conflict, "AI 模型限额作用域已存在。", "AI",
        ArgumentNames: ["providerKey", "modelName", "businessKey", "producerKey"]);
}
