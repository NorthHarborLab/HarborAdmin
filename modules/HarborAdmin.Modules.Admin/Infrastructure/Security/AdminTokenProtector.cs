using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HarborAdmin.Modules.Admin.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace HarborAdmin.Modules.Admin.Infrastructure.Security;

/// <summary>
/// Admin access token 保护器。
/// </summary>
public sealed class AdminTokenProtector(IOptions<AdminAuthOptions> options)
{
    private readonly byte[] _key = SHA256.HashData(Encoding.UTF8.GetBytes(options.Value.SigningKey));

    /// <summary>
    /// 创建 access token。
    /// </summary>
    /// <param name="userId">用户 ID。</param>
    /// <param name="userName">登录名。</param>
    /// <param name="expiresAt">过期时间。</param>
    /// <returns>签名后的 access token。</returns>
    public string CreateAccessToken(long userId, string userName, DateTimeOffset expiresAt)
    {
        var payload = new TokenPayload(userId, userName, expiresAt.ToUnixTimeSeconds());
        var json = JsonSerializer.Serialize(payload);
        var payloadText = Base64UrlEncode(Encoding.UTF8.GetBytes(json));
        var signature = Sign(payloadText);
        return $"{payloadText}.{signature}";
    }

    /// <summary>
    /// 校验 access token。
    /// </summary>
    /// <param name="token">access token。</param>
    /// <returns>有效载荷；无效或过期时返回 <see langword="null"/>。</returns>
    public TokenPayload? ValidateAccessToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var parts = token.Split('.', 2);
        if (parts.Length != 2 || !FixedTimeEquals(parts[1], Sign(parts[0])))
        {
            return null;
        }

        try
        {
            var payload = JsonSerializer.Deserialize<TokenPayload>(Encoding.UTF8.GetString(Base64UrlDecode(parts[0])));
            if (payload is null || payload.ExpiresAt < DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            {
                return null;
            }

            return payload;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 创建 refresh token 明文。
    /// </summary>
    /// <returns>refresh token。</returns>
    public string CreateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(48);
        return Base64UrlEncode(bytes);
    }

    /// <summary>
    /// 计算 refresh token 哈希。
    /// </summary>
    /// <param name="refreshToken">refresh token 明文。</param>
    /// <returns>哈希值。</returns>
    public string HashRefreshToken(string refreshToken)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));
        return Convert.ToHexString(hash);
    }

    /// <summary>
    /// 对 access token payload 做 HMAC 签名。
    /// </summary>
    private string Sign(string payloadText)
    {
        using var hmac = new HMACSHA256(_key);
        return Base64UrlEncode(hmac.ComputeHash(Encoding.UTF8.GetBytes(payloadText)));
    }

    /// <summary>
    /// 用固定时间比较签名，降低时序侧信道风险。
    /// </summary>
    private static bool FixedTimeEquals(string left, string right)
    {
        var leftBytes = Encoding.UTF8.GetBytes(left);
        var rightBytes = Encoding.UTF8.GetBytes(right);
        return leftBytes.Length == rightBytes.Length
            && CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }

    /// <summary>
    /// 将字节数组编码为 Base64Url 字符串。
    /// </summary>
    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    /// <summary>
    /// 将 Base64Url 字符串解码为字节数组。
    /// </summary>
    private static byte[] Base64UrlDecode(string value)
    {
        var text = value.Replace('-', '+').Replace('_', '/');
        text = text.PadRight(text.Length + (4 - text.Length % 4) % 4, '=');
        return Convert.FromBase64String(text);
    }
}
