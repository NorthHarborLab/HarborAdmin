namespace HarborAdmin.Modules.International.Contracts.Requests;

/// <summary>
/// AI 翻译国际化条目请求。
/// </summary>
public sealed record TranslateInternationalEntryRequest(
    IReadOnlyList<string> TargetLocales,
    string? Model = null,
    string? PromptOverride = null,
    string? KnowledgeText = null,
    string? KnowledgeTextMode = null);
