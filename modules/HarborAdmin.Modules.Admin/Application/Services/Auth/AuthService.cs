using System.Security.Cryptography;
using System.Text;
using HarborAdmin.BuildingBlocks.Abstractions.Exception;
using HarborAdmin.BuildingBlocks.Caching.Abstractions;
using HarborAdmin.Modules.Admin.Application.Services.Captcha;
using HarborAdmin.Modules.Admin.Contracts.Auth.Dto;
using HarborAdmin.Modules.Admin.Contracts.Auth.Request;
using HarborAdmin.Modules.Admin.Contracts.Captcha.Dto;
using HarborAdmin.Modules.Admin.Domain.Entities;
using HarborAdmin.Modules.Admin.Infrastructure.Caching;
using HarborAdmin.Modules.Admin.Infrastructure.Contexts;
using HarborAdmin.Modules.Admin.Infrastructure.Options;
using HarborAdmin.Modules.Admin.Infrastructure.Security;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace HarborAdmin.Modules.Admin.Application.Services.Auth;

/// <summary>
/// Admin 匿名认证服务：加密挑战、验证码、登录与令牌刷新。
/// </summary>
public sealed class AuthService(
    IAdminDbContext db,
    AdminTokenProtector tokenProtector,
    IOptions<AdminAuthOptions> authOptions,
    IWebHostEnvironment environment,
    CaptchaChallengeService captchaChallengeService,
    IHarborCache cache)
{
    /// <summary>
    /// Refresh token Cookie 名称。
    /// </summary>
    private const string RefreshCookieName = "harbor_refresh_token";

    /// <summary>
    /// RSA 加密挑战有效分钟数。
    /// </summary>
    private static readonly TimeSpan CryptoChallengeExpiration = TimeSpan.FromMinutes(2);

    /// <summary>
    /// 用户密码哈希器。
    /// </summary>
    private readonly PasswordHasher<AdminUser> _passwordHasher = new();

    /// <summary>
    /// Admin 模块 ORM 实例。
    /// </summary>
    private IFreeSql Orm => db.Orm;

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
        var user = await Orm.Select<AdminUser>()
            .Where(item => item.UserName == userName)
            .ToOneAsync(cancellationToken);

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
            await Orm.Update<AdminUser>().SetSource(user).ExecuteAffrowsAsync(cancellationToken);
        }

        var accessToken = tokenProtector.CreateAccessToken(
            user.Id,
            user.UserName,
            DateTimeOffset.UtcNow.AddMinutes(authOptions.Value.AccessTokenMinutes));
        await IssueRefreshTokenAsync(user.Id, response, cancellationToken);
        return new LoginResultDto(accessToken);
    }

    /// <summary>
    /// 使用 refresh token Cookie 续期 access token。
    /// </summary>
    /// <param name="request">HTTP 请求，用于读取 refresh token。</param>
    /// <param name="response">HTTP 响应，用于轮换 refresh token。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>新的 access token。</returns>
    public async Task<RefreshTokenResultDto> RefreshAsync(HttpRequest request, HttpResponse response, CancellationToken cancellationToken)
    {
        if (!request.Cookies.TryGetValue(RefreshCookieName, out var refreshToken))
        {
            throw new UnauthorizedDomainException("刷新令牌不存在。");
        }

        var tokenHash = tokenProtector.HashRefreshToken(refreshToken);
        var token = await Orm.Select<AdminRefreshToken>()
            .Where(item => item.TokenHash == tokenHash)
            .ToOneAsync(cancellationToken);
        if (token is null || IsRefreshTokenRevoked(token) || token.ExpiresAt < DateTimeOffset.UtcNow)
        {
            throw new UnauthorizedDomainException("刷新令牌无效。");
        }

        var user = await GetUserAsync(token.UserId, cancellationToken);
        if (user is null || !user.Enabled)
        {
            throw new UnauthorizedDomainException("用户不存在或已禁用。");
        }

        token.RevokedAt = DateTimeOffset.UtcNow;
        await Orm.Update<AdminRefreshToken>().SetSource(token).ExecuteAffrowsAsync(cancellationToken);
        await IssueRefreshTokenAsync(user.Id, response, cancellationToken);
        var accessToken = tokenProtector.CreateAccessToken(
            user.Id,
            user.UserName,
            DateTimeOffset.UtcNow.AddMinutes(authOptions.Value.AccessTokenMinutes));
        return new RefreshTokenResultDto(accessToken);
    }

    /// <summary>
    /// 吊销 refresh token 并清除登录 Cookie。
    /// </summary>
    /// <param name="request">HTTP 请求。</param>
    /// <param name="response">HTTP 响应。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task LogoutAsync(HttpRequest request, HttpResponse response, CancellationToken cancellationToken)
    {
        if (request.Cookies.TryGetValue(RefreshCookieName, out var refreshToken))
        {
            var tokenHash = tokenProtector.HashRefreshToken(refreshToken);
            var token = await Orm.Select<AdminRefreshToken>()
                .Where(item => item.TokenHash == tokenHash)
                .ToOneAsync(cancellationToken);
            if (token is not null && !IsRefreshTokenRevoked(token))
            {
                token.RevokedAt = DateTimeOffset.UtcNow;
                await Orm.Update<AdminRefreshToken>().SetSource(token).ExecuteAffrowsAsync(cancellationToken);
            }
        }

        response.Cookies.Delete(RefreshCookieName);
    }

    /// <summary>
    /// 根据用户 ID 获取用户实体。
    /// </summary>
    /// <param name="userId">用户 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>用户实体；不存在时返回 <see langword="null"/>。</returns>
    private async Task<AdminUser?> GetUserAsync(long userId, CancellationToken cancellationToken) =>
        await Orm.Select<AdminUser>().Where(user => user.Id == userId).ToOneAsync(cancellationToken);

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
    /// 生成 refresh token 并写入数据库与 HttpOnly Cookie。
    /// </summary>
    /// <param name="userId">用户 ID。</param>
    /// <param name="response">HTTP 响应。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    private async Task IssueRefreshTokenAsync(long userId, HttpResponse response, CancellationToken cancellationToken)
    {
        var refreshToken = tokenProtector.CreateRefreshToken();
        await Orm.Insert(new AdminRefreshToken
        {
            UserId = userId,
            TokenHash = tokenProtector.HashRefreshToken(refreshToken),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(authOptions.Value.RefreshTokenDays),
            CreatedAt = DateTimeOffset.UtcNow,
        }).ExecuteAffrowsAsync(cancellationToken);

        response.Cookies.Append(RefreshCookieName, refreshToken, new CookieOptions
        {
            HttpOnly = true,
            IsEssential = true,
            SameSite = SameSiteMode.Lax,
            Secure = !environment.IsDevelopment(),
            Expires = DateTimeOffset.UtcNow.AddDays(authOptions.Value.RefreshTokenDays),
        });
    }

    /// <summary>
    /// 判断 refresh token 是否已吊销。
    /// </summary>
    /// <param name="token">刷新令牌实体。</param>
    /// <returns>已吊销时返回 <see langword="true"/>。</returns>
    private static bool IsRefreshTokenRevoked(AdminRefreshToken token) =>
        token.RevokedAt is { } revokedAt && revokedAt > DateTimeOffset.MinValue.AddYears(1);
}
