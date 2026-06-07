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
    public string CreateAccessToken(long userId, string userName, DateTimeOffset expiresAt)
    {
        var payload = new TokenPayload(userId, userName, expiresAt.ToUnixTimeSeconds());
        var json = JsonSerializer.Serialize(payload);
        var payloadText = Base64UrlEncode(Encoding.UTF8.GetBytes(json));
        var signature = Sign(payloadText);
        return $"{payloadText}.{signature}";
    }

    /// <summary>
    /// 验证 access token。
    /// </summary>
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
    /// 创建随机刷新令牌。
    /// </summary>
    public static string CreateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(48);
        return Base64UrlEncode(bytes);
    }

    /// <summary>
    /// 对刷新令牌做哈希。
    /// </summary>
    public static string HashRefreshToken(string refreshToken)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));
        return Convert.ToHexString(hash);
    }

    private string Sign(string payloadText)
    {
        using var hmac = new HMACSHA256(_key);
        return Base64UrlEncode(hmac.ComputeHash(Encoding.UTF8.GetBytes(payloadText)));
    }

    private static bool FixedTimeEquals(string left, string right)
    {
        var leftBytes = Encoding.UTF8.GetBytes(left);
        var rightBytes = Encoding.UTF8.GetBytes(right);
        return leftBytes.Length == rightBytes.Length
            && CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] Base64UrlDecode(string value)
    {
        var text = value.Replace('-', '+').Replace('_', '/');
        text = text.PadRight(text.Length + (4 - text.Length % 4) % 4, '=');
        return Convert.FromBase64String(text);
    }
}

/// <summary>
/// Access token 载荷。
/// </summary>
public sealed record TokenPayload(long UserId, string UserName, long ExpiresAt);
