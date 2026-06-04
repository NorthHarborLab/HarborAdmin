namespace HarborAdmin.Client.AI.Invocation;

/// <summary>
/// AI 引用。
/// </summary>
public sealed record AiReference(string Source, string Content, double? Score = null);


