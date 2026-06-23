using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using HarborAdmin.BuildingBlocks.Abstractions.Attributes;
using HarborAdmin.BuildingBlocks.Abstractions.Auth;
using HarborAdmin.BuildingBlocks.Abstractions.ModelResults;
using HarborAdmin.Modules.Admin.Application.Services.User;
using HarborAdmin.Modules.Admin.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace HarborAdmin.Host.Infrastructure.Security;

/// <summary>
/// 基于 JWT Profile 的标准认证处理器。
/// </summary>
public sealed class JwtProfileAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    JwtProfileTokenService tokenService,
    UserService userService,
    AdminRequestUser adminRequestUser,
    ClientJwtRequestPrincipal clientPrincipal)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    private const string FailureMessageItemKey = "__HarborJwtProfileFailureMessage";

    /// <inheritdoc />
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var endpoint = Context.GetEndpoint();
        if (endpoint?.Metadata.GetMetadata<IAllowAnonymous>() is not null)
        {
            return AuthenticateResult.NoResult();
        }

        var profileAttribute = endpoint?.Metadata.GetMetadata<JwtTokenProfileAttribute>();
        if (profileAttribute is null)
        {
            return AuthenticateResult.NoResult();
        }

        var token = ResolveBearerToken();
        if (string.IsNullOrWhiteSpace(token))
        {
            return Fail("未登录或登录已过期。");
        }

        var tokenResult = await tokenService.ValidateAccessTokenAsync(
            profileAttribute.ProfileKey,
            token,
            Context.RequestAborted);
        if (tokenResult is null)
        {
            return Fail("JWT 无效或已过期。");
        }

        if (IsAdminProfile(tokenResult.ProfileKey))
        {
            var adminResult = await ApplyAdminPrincipalAsync(tokenResult);
            if (adminResult is not null)
            {
                return adminResult;
            }
        }
        else
        {
            clientPrincipal.Set(
                tokenResult.ProfileKey,
                tokenResult.Subject,
                tokenResult.JwtId,
                tokenResult.Claims);
        }

        var principal = CreateClaimsPrincipal(tokenResult);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);
        return AuthenticateResult.Success(ticket);
    }

    /// <inheritdoc />
    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        if (Response.HasStarted)
        {
            return;
        }

        Response.StatusCode = StatusCodes.Status401Unauthorized;
        Response.ContentType = "application/json; charset=utf-8";
        var message = Context.Items.TryGetValue(FailureMessageItemKey, out var value)
            ? value?.ToString() ?? "未登录或登录已过期。"
            : "未登录或登录已过期。";
        await Response.WriteAsync(SerializeFailure(ApiResultCodes.Unauthorized, message), Context.RequestAborted);
    }

    /// <inheritdoc />
    protected override async Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        if (Response.HasStarted)
        {
            return;
        }

        Response.StatusCode = StatusCodes.Status403Forbidden;
        Response.ContentType = "application/json; charset=utf-8";
        await Response.WriteAsync(SerializeFailure(ApiResultCodes.Forbidden, "没有访问该接口的权限。"), Context.RequestAborted);
    }

    /// <summary>
    /// 应用后台用户主体。
    /// </summary>
    private async Task<AuthenticateResult?> ApplyAdminPrincipalAsync(JwtProfileTokenResult tokenResult)
    {
        if (tokenResult.Subject is null || !long.TryParse(tokenResult.Subject, out var userId))
        {
            return Fail("后台 JWT 主体无效。");
        }

        var user = await userService.GetUserAsync(userId, Context.RequestAborted);
        if (user is null || !user.Enabled)
        {
            return Fail("用户不存在或已禁用。");
        }

        adminRequestUser.Id = user.Id;
        adminRequestUser.UserName = user.UserName;
        adminRequestUser.DisplayName = user.DisplayName;
        return null;
    }

    /// <summary>
    /// 创建标准 ClaimsPrincipal。
    /// </summary>
    private ClaimsPrincipal CreateClaimsPrincipal(JwtProfileTokenResult tokenResult)
    {
        var claims = tokenResult.Claims
            .Select(item => new Claim(item.Key, item.Value))
            .ToList();
        claims.Add(new Claim("harbor:jwt_profile", tokenResult.ProfileKey));

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        return new ClaimsPrincipal(identity);
    }

    /// <summary>
    /// 解析 Bearer token。
    /// </summary>
    private string? ResolveBearerToken()
    {
        var authorization = Request.Headers.Authorization.ToString();
        return authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? authorization["Bearer ".Length..].Trim()
            : null;
    }

    /// <summary>
    /// 返回认证失败。
    /// </summary>
    private AuthenticateResult Fail(string message)
    {
        Context.Items[FailureMessageItemKey] = message;
        return AuthenticateResult.Fail(message);
    }

    /// <summary>
    /// 判断是否后台管理 Profile。
    /// </summary>
    private static bool IsAdminProfile(string profileKey) =>
        string.Equals(profileKey, JwtTokenProfileKeys.Admin, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// 序列化失败响应。
    /// </summary>
    private static string SerializeFailure(int code, string message) =>
        JsonSerializer.Serialize(ApiResult.Fail(code, message), new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        });
}
