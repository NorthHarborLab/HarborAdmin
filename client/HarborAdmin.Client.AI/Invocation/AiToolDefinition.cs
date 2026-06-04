namespace HarborAdmin.Client.AI.Invocation;

/// <summary>
/// AI 工具定义。
/// </summary>
public sealed record AiToolDefinition(
    string Name,
    string? Description = null,
    string? ParametersJson = null);


