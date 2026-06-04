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
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
