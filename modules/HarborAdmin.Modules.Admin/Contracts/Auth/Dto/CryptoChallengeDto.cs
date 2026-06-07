namespace HarborAdmin.Modules.Admin.Contracts.Auth.Dto;

/// <summary>
/// RSA 加密挑战
/// </summary>
public sealed record CryptoChallengeDto(
    string ChallengeId,
    string PublicKey,
    DateTimeOffset ExpiresAt);
