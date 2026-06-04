namespace HarborAdmin.Client.AI.Invocation;

/// <summary>
/// AI 消息内容片段。
/// </summary>
public sealed record AiMessageContentPart(
    string Type,
    string? Text = null,
    string? Url = null,
    string? FileUri = null,
    string? MimeType = null,
    string? ToolCallId = null,
    string? ToolName = null,
    string? ArgumentsJson = null,
    string? ResultJson = null,
    IReadOnlyDictionary<string, string>? Metadata = null);


