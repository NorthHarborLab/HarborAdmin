namespace HarborAdmin.Modules.Admin.Contracts.JwtProfile.Dto;

/// <summary>
/// JWT Profile DTO。
/// </summary>
public sealed record JwtProfileDto(
    long Id,
    string ProfileKey,
    string DisplayName,
    string Purpose,
    string Issuer,
    string Audience,
    string Algorithm,
    string SecretRef,
    int SecretVersion,
    int AccessTokenMinutes,
    int RefreshTokenDays,
    int ClockSkewSeconds,
    bool Enabled,
    string? Description,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
