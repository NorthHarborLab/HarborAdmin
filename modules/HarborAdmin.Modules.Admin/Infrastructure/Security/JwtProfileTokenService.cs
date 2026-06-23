using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using HarborAdmin.BuildingBlocks.Abstractions.Exception;
using HarborAdmin.BuildingBlocks.Abstractions.Secrets;
using HarborAdmin.Modules.Admin.Application.Abstractions;
using HarborAdmin.Modules.Admin.Domain.Entities;
using Microsoft.IdentityModel.Tokens;

namespace HarborAdmin.Modules.Admin.Infrastructure.Security;

/// <summary>
/// JWT Profile token 签发与校验服务。
/// </summary>
public sealed class JwtProfileTokenService(
    IAdminJwtProfileRepository profileRepository,
    IAdminJwtRefreshTokenRepository refreshTokenRepository,
    ISecretResolver secretResolver)
{
    private static readonly JwtSecurityTokenHandler TokenHandler = new() { MapInboundClaims = false };

    /// <summary>
    /// 获取必需的 JWT Profile。
    /// </summary>
    /// <param name="profileKey">Profile Key。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>已启用的 JWT Profile。</returns>
    public async Task<AdminJwtProfile> GetRequiredProfileAsync(string profileKey, CancellationToken cancellationToken = default)
    {
        var profile = await profileRepository.GetByProfileKeyAsync(profileKey, cancellationToken);
        if (profile is null)
        {
            throw new ValidationDomainException($"JWT Profile '{profileKey}' 未配置。");
        }

        if (!profile.Enabled)
        {
            throw new UnauthorizedDomainException($"JWT Profile '{profileKey}' 已禁用。");
        }

        if (!string.Equals(profile.Algorithm, AdminJwtProfileConstants.HmacSha256Algorithm, StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationDomainException($"JWT Profile '{profileKey}' 仅支持 HS256。");
        }

        return profile;
    }

    /// <summary>
    /// 签发 access token 与 refresh token。
    /// </summary>
    /// <param name="profileKey">Profile Key。</param>
    /// <param name="subject">主体标识。</param>
    /// <param name="subjectType">主体类型。</param>
    /// <param name="claims">JWT Claims。</param>
    /// <param name="createdByIp">创建来源 IP。</param>
    /// <param name="userAgent">User-Agent。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>token pair。</returns>
    public async Task<JwtProfileTokenPairResult> IssueTokenPairAsync(string profileKey, string subject, string subjectType, IEnumerable<Claim> claims,
        string? createdByIp, string? userAgent, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectType);

        var profile = await GetRequiredProfileAsync(profileKey, cancellationToken);
        var secret = await ResolveSigningSecretAsync(profile, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var accessTokenExpiresAt = now.AddMinutes(profile.AccessTokenMinutes);
        var refreshTokenExpiresAt = now.AddDays(profile.RefreshTokenDays);
        var accessToken = CreateToken(profile, BuildAccessTokenClaims(subject, claims, now), secret, now, accessTokenExpiresAt);
        var refreshToken = CreateRefreshToken();
        await refreshTokenRepository.InsertAsync(new AdminJwtRefreshToken
        {
            ProfileKey = profile.ProfileKey,
            Subject = subject,
            SubjectType = subjectType,
            TokenHash = HashRefreshToken(refreshToken),
            ExpiresAt = refreshTokenExpiresAt,
            CreatedAt = now,
            CreatedByIp = NormalizeOptional(createdByIp, 128),
            UserAgent = NormalizeOptional(userAgent, null),
        }, cancellationToken);

        return new JwtProfileTokenPairResult(
            profile.ProfileKey,
            subject,
            accessToken,
            refreshToken,
            accessTokenExpiresAt,
            refreshTokenExpiresAt);
    }

    /// <summary>
    /// 使用 refresh token 轮换并签发新的 token pair。
    /// </summary>
    /// <param name="refreshToken">Refresh token 明文。</param>
    /// <param name="expectedProfileKey">期望的 Profile Key。</param>
    /// <param name="claimsFactory">刷新主体 Claims 工厂。</param>
    /// <param name="createdByIp">创建来源 IP。</param>
    /// <param name="userAgent">User-Agent。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>新的 token pair。</returns>
    public async Task<JwtProfileTokenPairResult> RefreshTokenPairAsync(string refreshToken, string? expectedProfileKey,
        Func<JwtRefreshTokenSubjectContext, CancellationToken, Task<IEnumerable<Claim>>> claimsFactory,
        string? createdByIp, string? userAgent, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new UnauthorizedDomainException("刷新令牌无效。");
        }

        var oldHash = HashRefreshToken(refreshToken);
        var stored = await refreshTokenRepository.GetByTokenHashAsync(oldHash, cancellationToken);
        if (stored is null || IsRefreshTokenRevoked(stored) || stored.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            throw new UnauthorizedDomainException("刷新令牌无效。");
        }

        if (!string.IsNullOrWhiteSpace(expectedProfileKey)
            && !string.Equals(stored.ProfileKey, expectedProfileKey, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedDomainException("刷新令牌无效。");
        }

        var profile = await GetRequiredProfileAsync(stored.ProfileKey, cancellationToken);
        var claims = await claimsFactory(
            new JwtRefreshTokenSubjectContext(stored.ProfileKey, stored.Subject, stored.SubjectType),
            cancellationToken);
        var secret = await ResolveSigningSecretAsync(profile, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var accessTokenExpiresAt = now.AddMinutes(profile.AccessTokenMinutes);
        var refreshTokenExpiresAt = now.AddDays(profile.RefreshTokenDays);
        var newRefreshToken = CreateRefreshToken();
        var newRefreshTokenHash = HashRefreshToken(newRefreshToken);

        stored.RevokedAt = now;
        stored.ReplacedByTokenHash = newRefreshTokenHash;
        await refreshTokenRepository.UpdateAsync(stored, cancellationToken);

        await refreshTokenRepository.InsertAsync(new AdminJwtRefreshToken
        {
            ProfileKey = profile.ProfileKey,
            Subject = stored.Subject,
            SubjectType = stored.SubjectType,
            TokenHash = newRefreshTokenHash,
            ExpiresAt = refreshTokenExpiresAt,
            CreatedAt = now,
            CreatedByIp = NormalizeOptional(createdByIp, 128),
            UserAgent = NormalizeOptional(userAgent, null),
        }, cancellationToken);

        var accessToken = CreateToken(profile, BuildAccessTokenClaims(stored.Subject, claims, now), secret, now, accessTokenExpiresAt);
        return new JwtProfileTokenPairResult(
            profile.ProfileKey,
            stored.Subject,
            accessToken,
            newRefreshToken,
            accessTokenExpiresAt,
            refreshTokenExpiresAt);
    }

    /// <summary>
    /// 吊销 refresh token。
    /// </summary>
    /// <param name="refreshToken">Refresh token 明文。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>找到并吊销时返回 <see langword="true"/>。</returns>
    public async Task<bool> RevokeRefreshTokenAsync(string? refreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return false;
        }

        var hash = HashRefreshToken(refreshToken);
        var stored = await refreshTokenRepository.GetByTokenHashAsync(hash, cancellationToken);
        if (stored is null || IsRefreshTokenRevoked(stored))
        {
            return false;
        }

        stored.RevokedAt = DateTimeOffset.UtcNow;
        await refreshTokenRepository.UpdateAsync(stored, cancellationToken);
        return true;
    }

    /// <summary>
    /// 校验指定 Profile 的 access token。
    /// </summary>
    /// <param name="profileKey">Profile Key。</param>
    /// <param name="token">Bearer token 明文。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>校验成功的 JWT 主体；失败时返回 <see langword="null"/>。</returns>
    public async Task<JwtProfileTokenResult?> ValidateAccessTokenAsync(string profileKey, string? token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var profile = await profileRepository.GetByProfileKeyAsync(profileKey, cancellationToken);
        if (profile is not { Enabled: true }
            || !string.Equals(profile.Algorithm, AdminJwtProfileConstants.HmacSha256Algorithm, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        try
        {
            var secret = await ResolveSigningSecretAsync(profile, cancellationToken);
            var principal = TokenHandler.ValidateToken(token, CreateValidationParameters(profile, secret), out _);
            var claims = principal.Claims
                .GroupBy(claim => claim.Type, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First().Value, StringComparer.Ordinal);
            var subject = claims.GetValueOrDefault(JwtRegisteredClaimNames.Sub)
                          ?? claims.GetValueOrDefault(ClaimTypes.NameIdentifier);
            var jwtId = claims.GetValueOrDefault(JwtRegisteredClaimNames.Jti);
            return new JwtProfileTokenResult(profile.ProfileKey, subject, jwtId, claims);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 解析签名密钥。
    /// </summary>
    private async Task<string> ResolveSigningSecretAsync(AdminJwtProfile profile, CancellationToken cancellationToken)
    {
        var secret = await secretResolver.ResolveAsync(profile.SecretRef, profile.SecretVersion, cancellationToken);
        if (string.IsNullOrWhiteSpace(secret))
        {
            throw new ValidationDomainException($"JWT Profile '{profile.ProfileKey}' 的签名密钥不可用。");
        }

        return secret;
    }

    /// <summary>
    /// 创建 refresh token 明文。
    /// </summary>
    private static string CreateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(48);
        return Base64UrlEncode(bytes);
    }

    /// <summary>
    /// 计算 refresh token 哈希。
    /// </summary>
    private static string HashRefreshToken(string refreshToken)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));
        return Convert.ToHexString(hash);
    }

    /// <summary>
    /// 创建 JWT。
    /// </summary>
    private static string CreateToken(
        AdminJwtProfile profile,
        IEnumerable<Claim> claims,
        string secret,
        DateTimeOffset notBefore,
        DateTimeOffset expiresAt)
    {
        var token = new JwtSecurityToken(
            profile.Issuer,
            profile.Audience,
            claims,
            notBefore.UtcDateTime,
            expiresAt.UtcDateTime,
            new SigningCredentials(CreateSecurityKey(secret), SecurityAlgorithms.HmacSha256));
        return TokenHandler.WriteToken(token);
    }

    /// <summary>
    /// 创建 JWT 校验参数。
    /// </summary>
    private static TokenValidationParameters CreateValidationParameters(AdminJwtProfile profile, string secret) =>
        new()
        {
            ClockSkew = TimeSpan.FromSeconds(profile.ClockSkewSeconds),
            IssuerSigningKey = CreateSecurityKey(secret),
            ValidateAudience = true,
            ValidateIssuer = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            ValidAudience = profile.Audience,
            ValidIssuer = profile.Issuer,
        };

    /// <summary>
    /// 创建 HMAC SHA-256 安全密钥。
    /// </summary>
    private static SymmetricSecurityKey CreateSecurityKey(string secret) => new(SHA256.HashData(Encoding.UTF8.GetBytes(secret)));

    /// <summary>
    /// 构造 access token claims。
    /// </summary>
    private static IReadOnlyList<Claim> BuildAccessTokenClaims(string subject, IEnumerable<Claim> claims, DateTimeOffset issuedAt)
    {
        var result = claims
            .Where(claim => !IsReservedTokenClaim(claim.Type))
            .ToList();
        result.Add(new Claim(JwtRegisteredClaimNames.Sub, subject));
        if (result.All(claim => claim.Type != ClaimTypes.NameIdentifier))
        {
            result.Add(new Claim(ClaimTypes.NameIdentifier, subject));
        }

        result.Add(new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")));
        result.Add(new Claim(JwtRegisteredClaimNames.Iat, issuedAt.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64));
        return result;
    }

    /// <summary>
    /// 判断是否 refresh token 已吊销。
    /// </summary>
    private static bool IsRefreshTokenRevoked(AdminJwtRefreshToken token) =>
        token.RevokedAt is { } revokedAt && revokedAt > DateTimeOffset.MinValue.AddYears(1);

    /// <summary>
    /// 判断是否系统保留 Claim。
    /// </summary>
    private static bool IsReservedTokenClaim(string claimType) =>
        claimType is JwtRegisteredClaimNames.Sub
            or JwtRegisteredClaimNames.Jti
            or JwtRegisteredClaimNames.Iat;

    /// <summary>
    /// 规范化可选文本。
    /// </summary>
    private static string? NormalizeOptional(string? value, int? maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        return maxLength.HasValue && normalized.Length > maxLength.Value
            ? normalized[..maxLength.Value]
            : normalized;
    }

    /// <summary>
    /// 将字节数组编码为 Base64Url 字符串。
    /// </summary>
    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}