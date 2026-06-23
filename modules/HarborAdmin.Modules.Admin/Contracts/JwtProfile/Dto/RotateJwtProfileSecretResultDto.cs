namespace HarborAdmin.Modules.Admin.Contracts.JwtProfile.Dto;

/// <summary>
/// JWT Profile 密钥轮换结果。
/// </summary>
public sealed record RotateJwtProfileSecretResultDto(
    JwtProfileDto Profile,
    string? GeneratedSecretValue);
