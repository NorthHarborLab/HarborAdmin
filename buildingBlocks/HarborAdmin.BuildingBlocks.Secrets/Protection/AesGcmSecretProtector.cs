using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace HarborAdmin.BuildingBlocks.Secrets.Protection;

/// <summary>
/// AES-GCM 密钥保护器。
/// </summary>
public sealed class AesGcmSecretProtector(IConfiguration configuration) : ISecretProtector
{
    private const int NonceSize = 12;
    private const int TagSize = 16;

    /// <summary>
    /// 配置键。
    /// </summary>
    public const string ProtectionKeyConfigurationKey = "Harbor:Secrets:ProtectionKey";

    /// <inheritdoc />
    public string Protect(string value)
    {
        var plain = Encoding.UTF8.GetBytes(value);
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var cipher = new byte[plain.Length];
        var tag = new byte[TagSize];
        using var aes = new AesGcm(GetKey(), TagSize);
        aes.Encrypt(nonce, plain, cipher, tag);
        return string.Join('.', Convert.ToBase64String(nonce), Convert.ToBase64String(tag), Convert.ToBase64String(cipher));
    }

    /// <inheritdoc />
    public string Unprotect(string cipherText)
    {
        var parts = cipherText.Split('.');
        if (parts.Length != 3)
        {
            throw new InvalidOperationException("Secret cipher text is invalid.");
        }

        var nonce = Convert.FromBase64String(parts[0]);
        var tag = Convert.FromBase64String(parts[1]);
        var cipher = Convert.FromBase64String(parts[2]);
        var plain = new byte[cipher.Length];
        using var aes = new AesGcm(GetKey(), TagSize);
        aes.Decrypt(nonce, cipher, tag, plain);
        return Encoding.UTF8.GetString(plain);
    }

    private byte[] GetKey()
    {
        var seed = configuration[ProtectionKeyConfigurationKey];
        if (string.IsNullOrWhiteSpace(seed))
        {
            seed = $"{Environment.MachineName}:HarborAdmin.Secrets:Development";
        }

        return SHA256.HashData(Encoding.UTF8.GetBytes(seed));
    }
}
