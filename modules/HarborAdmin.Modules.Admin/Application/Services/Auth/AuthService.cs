using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using HarborAdmin.BuildingBlocks.Abstractions.Exception;
using HarborAdmin.BuildingBlocks.Caching.Abstractions;
using HarborAdmin.Modules.Admin.Application.Abstractions;
using HarborAdmin.Modules.Admin.Application.Services.Captcha;
using HarborAdmin.Modules.Admin.Contracts.Auth.Dto;
using HarborAdmin.Modules.Admin.Contracts.Auth.Request;
using HarborAdmin.Modules.Admin.Contracts.Captcha.Dto;
using HarborAdmin.Modules.Admin.Domain.Entities;
using HarborAdmin.Modules.Admin.Infrastructure.Caching;
using HarborAdmin.Modules.Admin.Infrastructure.Security;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Hosting;

namespace HarborAdmin.Modules.Admin.Application.Services.Auth;

/// <summary>
/// Admin 匿名认证服务：加密挑战、验证码、登录与令牌刷新。
/// </summary>
public sealed class AuthService(
    IAdminAuthRepository repository,
    JwtProfileTokenService jwtTokenService,
    IWebHostEnvironment environment,
    CaptchaChallengeService captchaChallengeService,
    IHarborCache cache)
{
    /// <summary>
    /// Refresh token Cookie 名称。
    /// </summary>
    private const string RefreshCookieName = "harbor_refresh_token";

    /// <summary>
    /// 后台用户主体类型。
    /// </summary>
    private const string AdminSubjectType = "AdminUser";

    /// <summary>
    /// RSA 加密挑战有效分钟数。
    /// </summary>
    private static readonly TimeSpan CryptoChallengeExpiration = TimeSpan.FromMinutes(2);

    /// <summary>
    /// 用户密码哈希器。
    /// </summary>
    private readonly PasswordHasher<AdminUser> _passwordHasher = new();

    /// <summary>
    /// 创建一次性 RSA 加密挑战，供前端加密登录密码使用。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>挑战标识、公钥与过期时间。</returns>
    public async Task<CryptoChallengeDto> CreateCryptoChallengeAsync(CancellationToken cancellationToken)
    {
        using var rsa = RSA.Create(2048);
        var id = Guid.NewGuid().ToString("N");
        var expiresAt = DateTimeOffset.UtcNow.Add(CryptoChallengeExpiration);
        await cache.SetAsync(
            model => model.ChallengeId == id,
            new CryptoChallengeCacheModel
            {
                ChallengeId = id,
                PrivateKeyBase64 = Convert.ToBase64String(rsa.ExportRSAPrivateKey()),
                ExpiresAt = expiresAt,
            },
            CryptoChallengeExpiration,
            cancellationToken);
        var publicKey = Convert.ToBase64String(rsa.ExportSubjectPublicKeyInfo());
        return new CryptoChallengeDto(id, publicKey, expiresAt);
    }

    /// <summary>
    /// 按当前配置创建验证码挑战。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>统一验证码挑战数据。</returns>
    public Task<CaptchaChallengeDto> CreateCaptchaAsync(CancellationToken cancellationToken) =>
        captchaChallengeService.CreateChallengeAsync(cancellationToken);

    /// <summary>
    /// 校验验证码并颁发一次性登录令牌。
    /// </summary>
    /// <param name="request">验证码校验请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>验证码令牌。</returns>
    public Task<VerifyCaptchaResult> VerifyCaptchaAsync(
        VerifyCaptchaRequest request,
        CancellationToken cancellationToken) =>
        captchaChallengeService.VerifyChallengeAsync(request, cancellationToken);

    /// <summary>
    /// 校验凭据并签发 access token，同时写入 refresh token Cookie。
    /// </summary>
    /// <param name="request">登录请求。</param>
    /// <param name="response">HTTP 响应，用于写入 refresh token。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>access token。</returns>
    public async Task<LoginResultDto> LoginAsync(LoginRequest request, HttpResponse response, CancellationToken cancellationToken)
    {
        await captchaChallengeService.ConsumeCaptchaTokenAsync(request.CaptchaToken, cancellationToken);
        var userName = request.Username;
        var password = await ResolvePasswordAsync(request, cancellationToken);
        var user = await repository.GetUserByUserNameAsync(userName, cancellationToken);

        if (user is null || !user.Enabled)
        {
            throw new UnauthorizedDomainException("用户名或密码错误。");
        }

        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (result == PasswordVerificationResult.Failed)
        {
            throw new UnauthorizedDomainException("用户名或密码错误。");
        }

        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = _passwordHasher.HashPassword(user, password);
            user.UpdatedAt = DateTimeOffset.UtcNow;
            await repository.UpdateUserPasswordHashAsync(user, cancellationToken);
        }

        var tokenPair = await jwtTokenService.IssueTokenPairAsync(
            AdminJwtProfileConstants.AdminProfileKey,
            user.Id.ToString(),
            AdminSubjectType,
            CreateAdminClaims(user),
            ResolveRemoteIp(response.HttpContext),
            ResolveUserAgent(response.HttpContext),
            cancellationToken);
        AppendRefreshCookie(response, tokenPair.RefreshToken, tokenPair.RefreshTokenExpiresAt);
        return ToLoginResult(tokenPair);
    }

    /// <summary>
    /// 使用 refresh token Cookie 续期 access token。
    /// </summary>
    /// <param name="request">刷新 token 请求。</param>
    /// <param name="httpRequest">HTTP 请求，用于读取 refresh token Cookie。</param>
    /// <param name="response">HTTP 响应，用于轮换 refresh token。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>新的 access token。</returns>
    public async Task<RefreshTokenResultDto> RefreshAsync(
        RefreshTokenRequest? request,
        HttpRequest httpRequest,
        HttpResponse response,
        CancellationToken cancellationToken)
    {
        var refreshToken = ResolveRefreshToken(request?.RefreshToken, httpRequest);
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new UnauthorizedDomainException("刷新令牌不存在。");
        }

        var tokenPair = await jwtTokenService.RefreshTokenPairAsync(
            refreshToken,
            AdminJwtProfileConstants.AdminProfileKey,
            CreateAdminClaimsForRefreshAsync,
            ResolveRemoteIp(response.HttpContext),
            ResolveUserAgent(response.HttpContext),
            cancellationToken);
        AppendRefreshCookie(response, tokenPair.RefreshToken, tokenPair.RefreshTokenExpiresAt);
        return ToRefreshTokenResult(tokenPair);
    }

    /// <summary>
    /// 吊销 refresh token 并清除登录 Cookie。
    /// </summary>
    /// <param name="request">登出请求。</param>
    /// <param name="httpRequest">HTTP 请求，用于读取 refresh token Cookie。</param>
    /// <param name="response">HTTP 响应。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task LogoutAsync(
        LogoutRequest? request,
        HttpRequest httpRequest,
        HttpResponse response,
        CancellationToken cancellationToken)
    {
        var refreshToken = ResolveRefreshToken(request?.RefreshToken, httpRequest);
        await jwtTokenService.RevokeRefreshTokenAsync(refreshToken, cancellationToken);

        response.Cookies.Delete(RefreshCookieName);
    }

    /// <summary>
    /// 根据用户 ID 获取用户实体。
    /// </summary>
    /// <param name="userId">用户 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>用户实体；不存在时返回 <see langword="null"/>。</returns>
    private Task<AdminUser?> GetUserAsync(long userId, CancellationToken cancellationToken) =>
        repository.GetUserByIdAsync(userId, cancellationToken);

    /// <summary>
    /// 解析登录密码：优先 RSA 密文，开发环境允许明文回退。
    /// </summary>
    /// <param name="request">登录请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>明文密码。</returns>
    private async Task<string> ResolvePasswordAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.PasswordCipherText))
        {
            if (string.IsNullOrWhiteSpace(request.CryptoChallengeId))
            {
                throw new ValidationDomainException("密码加密挑战已过期。");
            }

            var state = await cache.TryConsumeAsync<CryptoChallengeCacheModel>(
                model => model.ChallengeId == request.CryptoChallengeId,
                cancellationToken);
            if (state is null || state.ExpiresAt < DateTimeOffset.UtcNow)
            {
                throw new ValidationDomainException("密码加密挑战已过期。");
            }

            using var rsa = RSA.Create();
            rsa.ImportRSAPrivateKey(Convert.FromBase64String(state.PrivateKeyBase64), out _);
            var cipherBytes = Convert.FromBase64String(request.PasswordCipherText);
            return Encoding.UTF8.GetString(rsa.Decrypt(cipherBytes, RSAEncryptionPadding.OaepSHA256));
        }

        if (environment.IsDevelopment() && !string.IsNullOrWhiteSpace(request.Password))
        {
            return request.Password;
        }

        throw new ValidationDomainException("密码传输格式无效。");
    }

    /// <summary>
    /// 创建后台用户 Claims。
    /// </summary>
    private static IEnumerable<Claim> CreateAdminClaims(AdminUser user) =>
    [
        new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName),
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Name, user.UserName),
    ];

    /// <summary>
    /// 为刷新后的后台用户创建 Claims。
    /// </summary>
    private async Task<IEnumerable<Claim>> CreateAdminClaimsForRefreshAsync(
        JwtRefreshTokenSubjectContext context,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(context.ProfileKey, AdminJwtProfileConstants.AdminProfileKey, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(context.SubjectType, AdminSubjectType, StringComparison.OrdinalIgnoreCase)
            || !long.TryParse(context.Subject, out var userId))
        {
            throw new UnauthorizedDomainException("刷新令牌无效。");
        }

        var user = await GetUserAsync(userId, cancellationToken);
        if (user is null || !user.Enabled)
        {
            throw new UnauthorizedDomainException("用户不存在或已禁用。");
        }

        return CreateAdminClaims(user);
    }

    /// <summary>
    /// 解析 refresh token。
    /// </summary>
    private static string? ResolveRefreshToken(string? requestToken, HttpRequest request) =>
        !string.IsNullOrWhiteSpace(requestToken)
            ? requestToken.Trim()
            : request.Cookies.TryGetValue(RefreshCookieName, out var cookieToken)
                ? cookieToken
                : null;

    /// <summary>
    /// 写入 refresh token Cookie。
    /// </summary>
    private void AppendRefreshCookie(HttpResponse response, string refreshToken, DateTimeOffset expiresAt)
    {
        response.Cookies.Append(RefreshCookieName, refreshToken, new CookieOptions
        {
            HttpOnly = true,
            IsEssential = true,
            SameSite = SameSiteMode.Lax,
            Secure = !environment.IsDevelopment(),
            Expires = expiresAt,
        });
    }

    /// <summary>
    /// 转换登录结果。
    /// </summary>
    private static LoginResultDto ToLoginResult(JwtProfileTokenPairResult tokenPair) =>
        new(
            tokenPair.AccessToken,
            tokenPair.RefreshToken,
            tokenPair.AccessTokenExpiresAt,
            tokenPair.RefreshTokenExpiresAt);

    /// <summary>
    /// 转换刷新结果。
    /// </summary>
    private static RefreshTokenResultDto ToRefreshTokenResult(JwtProfileTokenPairResult tokenPair) =>
        new(
            tokenPair.AccessToken,
            tokenPair.RefreshToken,
            tokenPair.AccessTokenExpiresAt,
            tokenPair.RefreshTokenExpiresAt);

    /// <summary>
    /// 解析请求来源 IP。
    /// </summary>
    private static string? ResolveRemoteIp(HttpContext context) =>
        context.Connection.RemoteIpAddress?.ToString();

    /// <summary>
    /// 解析 User-Agent。
    /// </summary>
    private static string? ResolveUserAgent(HttpContext context) =>
        context.Request.Headers.UserAgent.ToString();
}