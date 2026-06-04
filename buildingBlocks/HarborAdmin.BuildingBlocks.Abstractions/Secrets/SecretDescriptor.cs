namespace HarborAdmin.BuildingBlocks.Abstractions.Secrets;

/// <summary>
/// 密钥描述。
/// </summary>
public sealed record SecretDescriptor(
    long Id,
    string SecretRef,
    string DisplayName,
    int Version,
    bool Enabled,
    bool SecretConfigured,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>
/// 密钥版本描述。
/// </summary>
public sealed record SecretVersionDescriptor(
    long Id,
    string SecretRef,
    int Version,
    DateTimeOffset CreatedAt);
