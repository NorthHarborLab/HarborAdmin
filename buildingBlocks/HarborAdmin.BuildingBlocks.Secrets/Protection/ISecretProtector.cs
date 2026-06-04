namespace HarborAdmin.BuildingBlocks.Secrets.Protection;

/// <summary>
/// 密钥加解密器。
/// </summary>
public interface ISecretProtector
{
    /// <summary>
    /// 加密。
    /// </summary>
    string Protect(string value);

    /// <summary>
    /// 解密。
    /// </summary>
    string Unprotect(string cipherText);
}
