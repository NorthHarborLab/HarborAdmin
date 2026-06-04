namespace HarborAdmin.Client.AI.Invocation;

/// <summary>
/// AI 消息。
/// </summary>
public sealed record AiMessage(string Role, string? Content, IReadOnlyList<AiMessageContentPart>? Parts = null);


