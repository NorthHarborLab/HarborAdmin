namespace HarborAdmin.Modules.AI.Contracts.Requests;

/// <summary>
/// 保存 AI Prompt 请求。
/// </summary>
public sealed record SaveAiPromptRequest(
    string PromptKey,
    string Name,
    int Version,
    string SystemPromptMarkdown,
    string UserPromptMarkdown,
    string? VariablesJson,
    bool Enabled);

