namespace HarborAdmin.Modules.AI.Contracts.Snapshots;

/// <summary>
/// 已发布 Prompt。
/// </summary>
public sealed record AiPromptSnapshot(
    string PromptKey,
    int Version,
    string SystemPromptMarkdown,
    string UserPromptMarkdown,
    string? VariablesJson);
