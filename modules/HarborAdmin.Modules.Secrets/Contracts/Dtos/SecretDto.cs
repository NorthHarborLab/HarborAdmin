namespace HarborAdmin.Modules.Secrets.Contracts.Dtos;

/// <summary>
/// 通用密钥 DTO，不包含明文。
/// </summary>
public sealed record SecretDto(
    long Id,
    string SecretRef,
    string DisplayName,
    int Version,
    bool Enabled,
    bool SecretConfigured,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
