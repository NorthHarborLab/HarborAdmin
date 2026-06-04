namespace HarborAdmin.Client.AI.Invocation;

/// <summary>
/// AI 工具选项。
/// </summary>
public sealed record AiToolOptions(
    IReadOnlyList<AiToolDefinition>? Tools = null,
    string? ToolChoice = null,
    int MaxToolRounds = 0);


