using HarborAdmin.BuildingBlocks.Abstractions.Results;

namespace HarborAdmin.Modules.AI.Contracts.Shared.ErrorCode;

/// <summary>
/// AI 业务错误码。
/// </summary>
public static class AiBusinessErrorCodes
{
    /// <summary>
    /// 业务不存在。
    /// </summary>
    public static readonly HarborErrorDefinition NotFound = new(
        "AI.BUSINESS.NOT_FOUND", HarborErrorKind.NotFound, "AI 业务不存在。", "AI", ArgumentNames: ["id"]);

    /// <summary>
    /// 业务输入无效。
    /// </summary>
    public static readonly HarborErrorDefinition InvalidInput = new(
        "AI.BUSINESS.INVALID_INPUT", HarborErrorKind.Validation, "AI 业务输入无效。", "AI", ArgumentNames: ["reason"]);

    /// <summary>
    /// 业务 Key 已存在。
    /// </summary>
    public static readonly HarborErrorDefinition DuplicateKey = new(
        "AI.BUSINESS.DUPLICATE_KEY", HarborErrorKind.Conflict, "AI 业务 Key 已存在。", "AI", ArgumentNames: ["businessKey"]);
}
