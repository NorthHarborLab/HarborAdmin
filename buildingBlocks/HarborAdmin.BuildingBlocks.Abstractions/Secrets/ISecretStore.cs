namespace HarborAdmin.BuildingBlocks.Abstractions.Secrets;

/// <summary>
/// 密钥存储。
/// </summary>
public interface ISecretStore : ISecretResolver
{
    /// <summary>
    /// 保存或轮换密钥。
    /// </summary>
    Task<SecretDescriptor> SaveAsync(string secretRef, string displayName, string secretValue, CancellationToken cancellationToken = default);
}
