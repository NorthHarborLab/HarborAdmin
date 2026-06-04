namespace HarborAdmin.BuildingBlocks.Secrets.Contracts;

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

/// <summary>
/// 保存或轮换密钥请求。
/// </summary>
public sealed record SaveSecretRequest(string SecretRef, string DisplayName, string SecretValue);

/// <summary>
/// 设置密钥启停请求。
/// </summary>
public sealed record SetSecretEnabledRequest(string SecretRef, bool Enabled);
