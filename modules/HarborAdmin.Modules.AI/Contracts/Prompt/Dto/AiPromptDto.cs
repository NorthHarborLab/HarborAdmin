namespace HarborAdmin.Modules.AI.Contracts.Prompt.Dto;

/// <summary>
/// AI Prompt DTO。
/// </summary>
public sealed record AiPromptDto(
    long Id,
    string PromptKey,
    string Name,
    int Version,
    string SystemPromptMarkdown,
    string UserPromptMarkdown,
    string? VariablesJson,
    bool Enabled,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

