namespace HarborAdmin.Client.AI.Invocation;

/// <summary>
/// AI 输出选项。
/// </summary>
public sealed record AiOutputOptions(
    string? ResponseFormat = null,
    string? JsonSchema = null,
    bool Strict = false,
    bool ValidateAndRetry = false,
    int MaxRetryCount = 0);


