using HarborAdmin.BuildingBlocks.Abstractions.Results;

namespace HarborAdmin.Modules.AI.Contracts.Shared.ErrorCode;

/// <summary>
/// AI 知识库错误码。
/// </summary>
public static class AiKnowledgeBaseErrorCodes
{
    /// <summary>
    /// 知识库不存在。
    /// </summary>
    public static readonly HarborErrorDefinition NotFound = new(
        "AI.KNOWLEDGE_BASE.NOT_FOUND", HarborErrorKind.NotFound, "AI 知识库不存在。", "AI", ArgumentNames: ["id"]);

    /// <summary>
    /// 知识库输入无效。
    /// </summary>
    public static readonly HarborErrorDefinition InvalidInput = new(
        "AI.KNOWLEDGE_BASE.INVALID_INPUT", HarborErrorKind.Validation, "AI 知识库输入无效。", "AI", ArgumentNames: ["reason"]);

    /// <summary>
    /// 知识库 Key 已存在。
    /// </summary>
    public static readonly HarborErrorDefinition DuplicateKey = new(
        "AI.KNOWLEDGE_BASE.DUPLICATE_KEY", HarborErrorKind.Conflict, "AI 知识库 Key 已存在。", "AI", ArgumentNames: ["knowledgeKey"]);
}
