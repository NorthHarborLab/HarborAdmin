using System.Security.Cryptography;
using HarborAdmin.BuildingBlocks.Abstractions.Exception;
using HarborAdmin.BuildingBlocks.Abstractions.Secrets;
using HarborAdmin.Modules.Admin.Application.Abstractions;
using HarborAdmin.Modules.Admin.Contracts.JwtProfile.Dto;
using HarborAdmin.Modules.Admin.Contracts.JwtProfile.Request;
using HarborAdmin.Modules.Admin.Domain.Entities;
using HarborAdmin.Modules.Admin.Infrastructure.Security;

namespace HarborAdmin.Modules.Admin.Application.Services.JwtProfile;

/// <summary>
/// JWT Profile 管理服务。
/// </summary>
public sealed class JwtProfileService(IAdminJwtProfileRepository repository, ISecretStore secretStore)
{
    /// <summary>
    /// 列出 JWT Profile。
    /// </summary>
    public async Task<IReadOnlyList<JwtProfileDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var profiles = await repository.ListAsync(cancellationToken);
        return profiles.Select(ToDto).ToList();
    }

    /// <summary>
    /// 保存 JWT Profile。
    /// </summary>
    public async Task<JwtProfileDto> SaveAsync(string? profileKey, SaveJwtProfileRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedKey = NormalizeProfileKey(profileKey ?? request.ProfileKey);
        if (!string.Equals(normalizedKey, NormalizeProfileKey(request.ProfileKey), StringComparison.Ordinal))
        {
            throw new ValidationDomainException("Profile Key 与路由参数不一致。");
        }

        var existing = await repository.GetByProfileKeyAsync(normalizedKey, cancellationToken);
        if (IsAdminProfile(normalizedKey) && !request.Enabled)
        {
            throw new ValidationDomainException("后台管理 JWT Profile 不能禁用。");
        }

        await EnsureSecretVersionAsync(request.SecretRef, request.SecretVersion, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var profile = existing ?? new AdminJwtProfile
        {
            ProfileKey = normalizedKey,
            CreatedAt = now,
        };
        profile.DisplayName = request.DisplayName.Trim();
        profile.Purpose = request.Purpose.Trim();
        profile.Issuer = request.Issuer.Trim();
        profile.Audience = request.Audience.Trim();
        profile.Algorithm = AdminJwtProfileConstants.HmacSha256Algorithm;
        profile.SecretRef = request.SecretRef.Trim();
        profile.SecretVersion = request.SecretVersion;
        profile.AccessTokenMinutes = request.AccessTokenMinutes;
        profile.RefreshTokenDays = request.RefreshTokenDays;
        profile.ClockSkewSeconds = request.ClockSkewSeconds;
        profile.Enabled = request.Enabled;
        profile.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        profile.UpdatedAt = now;

        await repository.SaveAsync(profile, existing is not null, cancellationToken);
        return ToDto(profile);
    }

    /// <summary>
    /// 轮换 JWT Profile 签名密钥。
    /// </summary>
    public async Task<RotateJwtProfileSecretResultDto> RotateSecretAsync(
        string profileKey,
        RotateJwtProfileSecretRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalizedKey = NormalizeProfileKey(profileKey);
        var profile = await repository.GetByProfileKeyAsync(normalizedKey, cancellationToken)
                      ?? throw new NotFoundDomainException($"JWT Profile '{normalizedKey}' 不存在。");
        var generated = string.IsNullOrWhiteSpace(request.SecretValue)
            ? Convert.ToBase64String(RandomNumberGenerator.GetBytes(64))
            : null;
        var secretValue = generated ?? request.SecretValue!.Trim();
        var secretRef = string.IsNullOrWhiteSpace(profile.SecretRef)
            ? BuildDefaultSecretRef(normalizedKey)
            : profile.SecretRef;
        var descriptor = await secretStore.SaveAsync(
            secretRef,
            $"{profile.DisplayName} JWT 签名密钥",
            secretValue,
            cancellationToken);

        profile.SecretRef = descriptor.SecretRef;
        profile.SecretVersion = descriptor.Version;
        profile.UpdatedAt = DateTimeOffset.UtcNow;
        await repository.UpdateAsync(profile, cancellationToken);
        return new RotateJwtProfileSecretResultDto(ToDto(profile), generated);
    }

    /// <summary>
    /// 设置 JWT Profile 启停状态。
    /// </summary>
    public async Task<JwtProfileDto> SetEnabledAsync(string profileKey, bool enabled, CancellationToken cancellationToken = default)
    {
        var normalizedKey = NormalizeProfileKey(profileKey);
        if (IsAdminProfile(normalizedKey) && !enabled)
        {
            throw new ValidationDomainException("后台管理 JWT Profile 不能禁用。");
        }

        var profile = await repository.GetByProfileKeyAsync(normalizedKey, cancellationToken)
                      ?? throw new NotFoundDomainException($"JWT Profile '{normalizedKey}' 不存在。");
        profile.Enabled = enabled;
        profile.UpdatedAt = DateTimeOffset.UtcNow;
        await repository.UpdateAsync(profile, cancellationToken);
        return ToDto(profile);
    }

    /// <summary>
    /// 转换为 DTO。
    /// </summary>
    private static JwtProfileDto ToDto(AdminJwtProfile profile) =>
        new(
            profile.Id,
            profile.ProfileKey,
            profile.DisplayName,
            profile.Purpose,
            profile.Issuer,
            profile.Audience,
            profile.Algorithm,
            profile.SecretRef,
            profile.SecretVersion,
            profile.AccessTokenMinutes,
            profile.RefreshTokenDays,
            profile.ClockSkewSeconds,
            profile.Enabled,
            profile.Description,
            profile.CreatedAt,
            profile.UpdatedAt);

    /// <summary>
    /// 确认 Secret 版本可用。
    /// </summary>
    private async Task EnsureSecretVersionAsync(string secretRef, int version, CancellationToken cancellationToken)
    {
        var descriptor = await secretStore.GetAsync(secretRef.Trim(), cancellationToken);
        if (descriptor is not { Enabled: true })
        {
            throw new ValidationDomainException("签名密钥不存在或未启用。");
        }

        var targetVersion = await secretStore.GetVersionAsync(secretRef.Trim(), version, cancellationToken);
        if (targetVersion is null)
        {
            throw new ValidationDomainException("签名密钥版本不存在。");
        }
    }

    /// <summary>
    /// 规范化 Profile Key。
    /// </summary>
    private static string NormalizeProfileKey(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ValidationDomainException("Profile Key 不能为空。");
        }

        return value.Trim();
    }

    /// <summary>
    /// 是否后台管理 Profile。
    /// </summary>
    private static bool IsAdminProfile(string profileKey) =>
        string.Equals(profileKey, AdminJwtProfileConstants.AdminProfileKey, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// 构造默认 Secret Ref。
    /// </summary>
    private static string BuildDefaultSecretRef(string profileKey) =>
        IsAdminProfile(profileKey)
            ? AdminJwtProfileConstants.AdminSigningSecretRef
            : $"Jwt.{profileKey}.SigningKey";
}