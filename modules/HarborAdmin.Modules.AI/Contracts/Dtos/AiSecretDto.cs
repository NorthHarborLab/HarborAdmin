namespace HarborAdmin.Modules.AI.Contracts.Dtos;

/// <summary>
/// AI 密钥 DTO。
/// </summary>
public sealed record AiSecretDto(
    long Id,
    string SecretRef,
    string DisplayName,
    int Version,
    bool Enabled,
    bool SecretConfigured,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>
/// 保存 AI 密钥请求。
/// </summary>
public sealed record SaveAiSecretRequest(string SecretRef, string DisplayName, string SecretValue);
