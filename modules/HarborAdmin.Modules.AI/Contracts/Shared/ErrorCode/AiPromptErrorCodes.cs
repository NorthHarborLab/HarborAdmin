using HarborAdmin.BuildingBlocks.Abstractions.Results;

namespace HarborAdmin.Modules.AI.Contracts.Shared.ErrorCode;

/// <summary>
/// AI Prompt 错误码。
/// </summary>
public static class AiPromptErrorCodes
{
    /// <summary>
    /// Prompt 不存在。
    /// </summary>
    public static readonly HarborErrorDefinition NotFound = new(
        "AI.PROMPT.NOT_FOUND", HarborErrorKind.NotFound, "AI Prompt 不存在。", "AI", ArgumentNames: ["id"]);

    /// <summary>
    /// Prompt 输入无效。
    /// </summary>
    public static readonly HarborErrorDefinition InvalidInput = new(
        "AI.PROMPT.INVALID_INPUT", HarborErrorKind.Validation, "AI Prompt 输入无效。", "AI", ArgumentNames: ["reason"]);

    /// <summary>
    /// Prompt Key 与版本已存在。
    /// </summary>
    public static readonly HarborErrorDefinition DuplicateVersion = new(
        "AI.PROMPT.DUPLICATE_VERSION", HarborErrorKind.Conflict, "AI Prompt Key 与版本已存在。", "AI", ArgumentNames: ["promptKey", "version"]);
}
